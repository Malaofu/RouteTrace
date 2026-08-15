using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using RouteTrace.Core.Routes;

namespace RouteTrace.Core.Gpx;

public static class GpxExporter
{
    private const string GpxNamespace = "http://www.topografix.com/GPX/1/1";

    public static async Task<GpxExportResult> ExportAsync(
        RouteDocument document,
        Stream output,
        string creator,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentException.ThrowIfNullOrWhiteSpace(creator);

        var settings = new XmlWriterSettings
        {
            Async = true,
            Encoding = new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            Indent = true,
            CloseOutput = false
        };

        await using XmlWriter writer = XmlWriter.Create(output, settings);
        await writer.WriteStartDocumentAsync();
        await writer.WriteStartElementAsync(null, "gpx", GpxNamespace);
        await writer.WriteAttributeStringAsync(null, "version", null, "1.1");
        await writer.WriteAttributeStringAsync(null, "creator", null, creator);
        LazyExtensionXml? scopedExtensions = document.UnsupportedExtensionXml as LazyExtensionXml;
        if (scopedExtensions is not null)
        {
            foreach ((string prefix, string namespaceName) in scopedExtensions.RootNamespaceDeclarations)
            {
                await writer.WriteAttributeStringAsync("xmlns", prefix, null, namespaceName);
            }
            foreach ((XName name, string value) in scopedExtensions.RootAttributes)
            {
                string? prefix = string.IsNullOrEmpty(name.NamespaceName)
                    ? null
                    : writer.LookupPrefix(name.NamespaceName);
                await writer.WriteAttributeStringAsync(prefix, name.LocalName, name.NamespaceName, value);
            }
        }

        if (document.Metadata is not null)
        {
            await WriteMetadataAsync(writer, document.Metadata, scopedExtensions, cancellationToken);
        }

        for (int waypointIndex = 0; waypointIndex < document.Waypoints.Count; waypointIndex++)
        {
            Waypoint waypoint = document.Waypoints[waypointIndex];
            await WritePointAsync(
                writer, "wpt", waypoint.Point, waypoint.Name,
                waypoint.Comment, waypoint.Description, waypoint.Symbol, waypoint.Links,
                scopedExtensions?.At(GpxExtensionScope.Waypoint, waypointIndex),
                scopedExtensions?.StandardChildrenAt(GpxExtensionScope.Waypoint, waypointIndex),
                cancellationToken);
        }

        for (int routeIndex = 0; routeIndex < document.Routes.Count; routeIndex++)
        {
            Route route = document.Routes[routeIndex];
            await writer.WriteStartElementAsync(null, "rte", GpxNamespace);
            IReadOnlyList<XElement>? routeStandardChildren = scopedExtensions?.StandardChildrenAt(
                GpxExtensionScope.Route, routeIndex);
            if (routeStandardChildren is null)
            {
                await WriteOptionalElementAsync(writer, "name", route.Name);
            }
            else
            {
                await WriteStandardChildrenWithTextReplacementsAsync(
                    writer, routeStandardChildren, cancellationToken, new Dictionary<string, string?> { ["name"] = route.Name });
            }
            if (scopedExtensions is null)
            {
                await WriteExtensionsAsync(writer, route.UnsupportedExtensionXml, cancellationToken);
            }
            else
            {
                await WritePreservedExtensionsAsync(
                    writer, scopedExtensions.At(GpxExtensionScope.Route, routeIndex), cancellationToken);
            }
            for (int pointIndex = 0; pointIndex < route.Points.Count; pointIndex++)
            {
                await WritePointAsync(
                    writer, "rtept", route.Points[pointIndex], null,
                    extensions: scopedExtensions?.At(GpxExtensionScope.RoutePoint, routeIndex, pointIndex),
                    standardChildren: scopedExtensions?.StandardChildrenAt(
                        GpxExtensionScope.RoutePoint, routeIndex, pointIndex),
                    cancellationToken: cancellationToken);
            }
            await writer.WriteEndElementAsync();
        }

        for (int trackIndex = 0; trackIndex < document.Tracks.Count; trackIndex++)
        {
            Track track = document.Tracks[trackIndex];
            await writer.WriteStartElementAsync(null, "trk", GpxNamespace);
            IReadOnlyList<XElement>? trackStandardChildren = scopedExtensions?.StandardChildrenAt(
                GpxExtensionScope.Track, trackIndex);
            if (trackStandardChildren is null)
            {
                await WriteOptionalElementAsync(writer, "name", track.Name);
                await WriteOptionalElementAsync(writer, "type", track.Type);
            }
            else
            {
                await WriteStandardChildrenWithTextReplacementsAsync(
                    writer, trackStandardChildren, cancellationToken,
                    new Dictionary<string, string?> { ["name"] = track.Name, ["type"] = track.Type });
            }
            await WritePreservedExtensionsAsync(
                writer, scopedExtensions?.At(GpxExtensionScope.Track, trackIndex) ?? [], cancellationToken);
            for (int segmentIndex = 0; segmentIndex < track.Segments.Count; segmentIndex++)
            {
                TrackSegment segment = track.Segments[segmentIndex];
                await writer.WriteStartElementAsync(null, "trkseg", GpxNamespace);
                for (int pointIndex = 0; pointIndex < segment.Points.Count; pointIndex++)
                {
                    await WritePointAsync(
                        writer, "trkpt", segment.Points[pointIndex], null,
                        extensions: scopedExtensions?.At(
                            GpxExtensionScope.TrackPoint, trackIndex, segmentIndex, pointIndex),
                        standardChildren: scopedExtensions?.StandardChildrenAt(
                            GpxExtensionScope.TrackPoint, trackIndex, segmentIndex, pointIndex),
                        cancellationToken: cancellationToken);
                }
                await WritePreservedExtensionsAsync(
                    writer,
                    scopedExtensions?.At(GpxExtensionScope.TrackSegment, trackIndex, segmentIndex) ?? [],
                    cancellationToken);
                await writer.WriteEndElementAsync();
            }
            await writer.WriteEndElementAsync();
        }

        IReadOnlyList<XElement>? preservedDocumentExtensions = scopedExtensions?.At(GpxExtensionScope.Document);
        IReadOnlyList<string>? documentExtensions = scopedExtensions is null ? WithoutOwnedExtensions(document) : null;
        if (preservedDocumentExtensions is { Count: > 0 } || documentExtensions is { Count: > 0 })
        {
            await writer.WriteStartElementAsync(null, "extensions", GpxNamespace);
            foreach (XElement extension in preservedDocumentExtensions ?? [])
            {
                WriteElement(writer, extension, cancellationToken);
            }
            foreach (string extensionXml in documentExtensions ?? [])
            {
                WriteExtension(writer, extensionXml, cancellationToken);
            }
            await writer.WriteEndElementAsync();
        }

        await writer.WriteEndElementAsync();
        await writer.WriteEndDocumentAsync();
        await writer.FlushAsync();

        return new GpxExportResult(document.UnsupportedExtensionXml.Count, []);
    }

    private static async Task WriteMetadataAsync(
        XmlWriter writer,
        RouteMetadata metadata,
        LazyExtensionXml? scopedExtensions,
        CancellationToken cancellationToken)
    {
        await writer.WriteStartElementAsync(null, "metadata", GpxNamespace);
        if (scopedExtensions is not null)
        {
            await WriteStandardChildrenWithTextReplacementsAsync(
                writer,
                scopedExtensions.StandardChildrenAt(GpxExtensionScope.Metadata),
                cancellationToken,
                new Dictionary<string, string?> { ["name"] = metadata.Name, ["desc"] = metadata.Description });
            await WritePreservedExtensionsAsync(
                writer, scopedExtensions.At(GpxExtensionScope.Metadata), cancellationToken);
            await writer.WriteEndElementAsync();
            return;
        }

        await WriteOptionalElementAsync(writer, "name", metadata.Name);
        await WriteOptionalElementAsync(writer, "desc", metadata.Description);
        if (metadata.Author is not null)
        {
            await WriteAuthorAsync(writer, metadata.Author);
        }
        foreach (RouteLink link in metadata.Links)
        {
            await writer.WriteStartElementAsync(null, "link", GpxNamespace);
            await writer.WriteAttributeStringAsync(null, "href", null, link.Href);
            await WriteOptionalElementAsync(writer, "text", link.Text);
            await WriteOptionalElementAsync(writer, "type", link.MimeType);
            await writer.WriteEndElementAsync();
        }
        if (metadata.Time is not null)
        {
            await writer.WriteElementStringAsync(null, "time", GpxNamespace, XmlConvert.ToString(metadata.Time.Value));
        }
        await WriteExtensionsAsync(writer, metadata.UnsupportedExtensionXml, cancellationToken);
        await writer.WriteEndElementAsync();
    }

    private static async Task WritePointAsync(
        XmlWriter writer,
        string elementName,
        RoutePoint point,
        string? name,
        string? comment = null,
        string? description = null,
        string? symbol = null,
        IReadOnlyList<RouteLink>? links = null,
        IReadOnlyList<XElement>? extensions = null,
        IReadOnlyList<XElement>? standardChildren = null,
        CancellationToken cancellationToken = default)
    {
        await writer.WriteStartElementAsync(null, elementName, GpxNamespace);
        await writer.WriteAttributeStringAsync(
            null, "lat", null, point.Coordinate.Latitude.ToString("F7", CultureInfo.InvariantCulture));
        await writer.WriteAttributeStringAsync(
            null, "lon", null, point.Coordinate.Longitude.ToString("F7", CultureInfo.InvariantCulture));
        if (standardChildren is not null)
        {
            if (point.ElevationMetres is not null)
            {
                await writer.WriteElementStringAsync(
                    null, "ele", GpxNamespace, FormatElevation(point.ElevationMetres.Value));
            }
            if (elementName == "wpt")
            {
                await WriteStandardChildrenWithTextReplacementsAsync(
                    writer, standardChildren, cancellationToken,
                    new Dictionary<string, string?> { ["name"] = name, ["desc"] = description }, "ele");
            }
            else
            {
                await WriteStandardChildrenAsync(writer, standardChildren, cancellationToken, "ele");
            }
            await WritePreservedExtensionsAsync(writer, extensions ?? [], cancellationToken);
            await writer.WriteEndElementAsync();
            return;
        }

        if (point.ElevationMetres is not null)
        {
            await writer.WriteElementStringAsync(
                null, "ele", GpxNamespace, FormatElevation(point.ElevationMetres.Value));
        }
        if (point.Time is not null)
        {
            await writer.WriteElementStringAsync(null, "time", GpxNamespace, XmlConvert.ToString(point.Time.Value));
        }
        await WriteOptionalElementAsync(writer, "name", name);
        await WriteOptionalElementAsync(writer, "cmt", comment);
        await WriteOptionalElementAsync(writer, "desc", description);
        foreach (RouteLink link in links ?? [])
        {
            await writer.WriteStartElementAsync(null, "link", GpxNamespace);
            await writer.WriteAttributeStringAsync(null, "href", null, link.Href);
            await WriteOptionalElementAsync(writer, "text", link.Text);
            await WriteOptionalElementAsync(writer, "type", link.MimeType);
            await writer.WriteEndElementAsync();
        }
        await WriteOptionalElementAsync(writer, "sym", symbol);
        await WritePreservedExtensionsAsync(writer, extensions ?? [], cancellationToken);
        await writer.WriteEndElementAsync();
    }

    private static async Task WriteAuthorAsync(XmlWriter writer, RouteAuthor author)
    {
        await writer.WriteStartElementAsync(null, "author", GpxNamespace);
        await WriteOptionalElementAsync(writer, "name", author.Name);
        if (!string.IsNullOrEmpty(author.EmailId) && !string.IsNullOrEmpty(author.EmailDomain))
        {
            await writer.WriteStartElementAsync(null, "email", GpxNamespace);
            await writer.WriteAttributeStringAsync(null, "id", null, author.EmailId);
            await writer.WriteAttributeStringAsync(null, "domain", null, author.EmailDomain);
            await writer.WriteEndElementAsync();
        }
        if (author.Link is not null)
        {
            await writer.WriteStartElementAsync(null, "link", GpxNamespace);
            await writer.WriteAttributeStringAsync(null, "href", null, author.Link.Href);
            await WriteOptionalElementAsync(writer, "text", author.Link.Text);
            await WriteOptionalElementAsync(writer, "type", author.Link.MimeType);
            await writer.WriteEndElementAsync();
        }
        await writer.WriteEndElementAsync();
    }

    private static async Task WriteExtensionsAsync(
        XmlWriter writer,
        IReadOnlyList<string> extensions,
        CancellationToken cancellationToken)
    {
        if (extensions.Count == 0) return;

        await writer.WriteStartElementAsync(null, "extensions", GpxNamespace);
        foreach (string extensionXml in extensions)
        {
            WriteExtension(writer, extensionXml, cancellationToken);
        }
        await writer.WriteEndElementAsync();
    }

    private static async Task WritePreservedExtensionsAsync(
        XmlWriter writer,
        IReadOnlyList<XElement> extensions,
        CancellationToken cancellationToken)
    {
        if (extensions.Count == 0) return;

        await writer.WriteStartElementAsync(null, "extensions", GpxNamespace);
        foreach (XElement extension in extensions)
        {
            WriteElement(writer, extension, cancellationToken);
        }
        await writer.WriteEndElementAsync();
    }

    private static IReadOnlyList<string> WithoutOwnedExtensions(RouteDocument document)
    {
        IEnumerable<string> ownedExtensions = (document.Metadata?.UnsupportedExtensionXml ?? [])
            .Concat(document.Routes.SelectMany(route => route.UnsupportedExtensionXml));
        var ownedCounts = ownedExtensions
            .GroupBy(CanonicalExtensionXml, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        var remaining = new List<string>();
        foreach (string xml in document.UnsupportedExtensionXml)
        {
            string key = CanonicalExtensionXml(xml);
            if (ownedCounts.TryGetValue(key, out int count) && count > 0)
            {
                ownedCounts[key] = count - 1;
            }
            else
            {
                remaining.Add(xml);
            }
        }
        return remaining;
    }

    private static string CanonicalExtensionXml(string xml) =>
        XElement.Parse(xml, LoadOptions.None).ToString(SaveOptions.DisableFormatting);

    private static void WriteExtension(
        XmlWriter writer,
        string extensionXml,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        XElement extension = XElement.Parse(extensionXml, LoadOptions.None);
        WriteElement(writer, extension, cancellationToken);
    }

    private static void WriteElement(XmlWriter writer, XElement element, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string? prefix = string.IsNullOrEmpty(element.Name.NamespaceName)
            ? null
            : writer.LookupPrefix(element.Name.NamespaceName) ?? DeclaredPrefix(element, element.Name.NamespaceName);
        writer.WriteStartElement(prefix, element.Name.LocalName, element.Name.NamespaceName);

        foreach (XAttribute declaration in element.Attributes().Where(attribute => attribute.IsNamespaceDeclaration))
        {
            string declaredPrefix = declaration.Name.LocalName == "xmlns" ? string.Empty : declaration.Name.LocalName;
            if (writer.LookupPrefix(declaration.Value) != declaredPrefix)
            {
                writer.WriteAttributeString("xmlns", declaredPrefix, null, declaration.Value);
            }
        }

        foreach (XAttribute attribute in element.Attributes().Where(attribute => !attribute.IsNamespaceDeclaration))
        {
            string? attributePrefix = string.IsNullOrEmpty(attribute.Name.NamespaceName)
                ? null
                : writer.LookupPrefix(attribute.Name.NamespaceName)
                    ?? DeclaredPrefix(element, attribute.Name.NamespaceName);
            writer.WriteAttributeString(
                attributePrefix, attribute.Name.LocalName, attribute.Name.NamespaceName, attribute.Value);
        }

        foreach (XNode node in element.Nodes())
        {
            if (node is XElement child) WriteElement(writer, child, cancellationToken);
            else node.WriteTo(writer);
        }
        writer.WriteEndElement();
    }

    private static string? DeclaredPrefix(XElement element, string namespaceName)
    {
        XAttribute? declaration = element.Attributes().FirstOrDefault(
            attribute => attribute.IsNamespaceDeclaration && attribute.Value == namespaceName);
        return declaration?.Name.LocalName == "xmlns" ? string.Empty : declaration?.Name.LocalName;
    }

    private static Task WriteStandardChildrenAsync(
        XmlWriter writer,
        IReadOnlyList<XElement> children,
        CancellationToken cancellationToken,
        params string[] excludedLocalNames)
    {
        foreach (XElement child in children)
        {
            if (excludedLocalNames.Contains(child.Name.LocalName, StringComparer.Ordinal)) continue;
            WriteElement(writer, child, cancellationToken);
        }
        return Task.CompletedTask;
    }

    private static async Task WriteStandardChildrenWithTextReplacementsAsync(
        XmlWriter writer,
        IReadOnlyList<XElement> children,
        CancellationToken cancellationToken,
        IReadOnlyDictionary<string, string?> replacements,
        params string[] excludedLocalNames)
    {
        var written = new HashSet<string>(StringComparer.Ordinal);
        foreach (XElement child in children)
        {
            string localName = child.Name.LocalName;
            if (excludedLocalNames.Contains(localName, StringComparer.Ordinal)) continue;
            if (!replacements.TryGetValue(localName, out string? replacement))
            {
                WriteElement(writer, child, cancellationToken);
                continue;
            }

            if (written.Add(localName)) await WriteOptionalElementAsync(writer, localName, replacement);
        }

        foreach ((string localName, string? replacement) in replacements)
        {
            if (written.Add(localName)) await WriteOptionalElementAsync(writer, localName, replacement);
        }
    }

    private static string FormatElevation(double elevation)
    {
        string value = elevation.ToString("R", CultureInfo.InvariantCulture);
        int exponentIndex = value.IndexOfAny(['E', 'e']);
        string significand = exponentIndex < 0 ? value : value[..exponentIndex];
        if (significand.Contains('.', StringComparison.Ordinal)) return value;

        return exponentIndex < 0
            ? value + ".0"
            : value.Insert(exponentIndex, ".0");
    }

    private static Task WriteOptionalElementAsync(XmlWriter writer, string name, string? value) =>
        string.IsNullOrEmpty(value)
            ? Task.CompletedTask
            : writer.WriteElementStringAsync(null, name, GpxNamespace, value);
}

public sealed record GpxExportResult(int RetainedExtensionCount, IReadOnlyList<string> OmittedExtensionNamespaces);
