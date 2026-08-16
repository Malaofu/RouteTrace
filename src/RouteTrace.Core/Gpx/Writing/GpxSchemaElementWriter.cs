using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using RouteTrace.Core.Gpx;
using RouteTrace.Core.Gpx.Preservation;
using RouteTrace.Core.Routes.Documents;
using RouteTrace.Core.Routes.Geometry;

namespace RouteTrace.Core.Gpx.Writing;

internal static class GpxSchemaElementWriter
{
    public static async Task WriteMetadataAsync(
        XmlWriter writer,
        RouteMetadata metadata,
        LazyExtensionXml? preservedXml,
        CancellationToken cancellationToken)
    {
        await writer.WriteStartElementAsync(null, "metadata", GpxXml.NamespaceName);
        if (preservedXml is not null)
        {
            await GpxPreservedContentWriter.WriteStandardChildrenWithTextReplacementsAsync(
                writer,
                preservedXml.StandardChildrenAt(GpxExtensionScope.Metadata),
                cancellationToken,
                new Dictionary<string, string?>
                {
                    ["name"] = metadata.Name,
                    ["desc"] = metadata.Description
                });
            await GpxPreservedContentWriter.WritePreservedExtensionsAsync(
                writer,
                preservedXml.At(GpxExtensionScope.Metadata),
                cancellationToken);
            await writer.WriteEndElementAsync();
            return;
        }

        await GpxWriterFormatting.WriteOptionalElementAsync(writer, "name", metadata.Name);
        await GpxWriterFormatting.WriteOptionalElementAsync(writer, "desc", metadata.Description);
        if (metadata.Author is not null)
        {
            await WriteAuthorAsync(writer, metadata.Author);
        }

        foreach (RouteLink link in metadata.Links)
        {
            await WriteLinkAsync(writer, link);
        }

        if (metadata.Time is not null)
        {
            await writer.WriteElementStringAsync(
                null,
                "time",
                GpxXml.NamespaceName,
                XmlConvert.ToString(metadata.Time.Value));
        }

        await GpxPreservedContentWriter.WriteExtensionXmlAsync(
            writer,
            metadata.UnsupportedExtensionXml,
            cancellationToken);
        await writer.WriteEndElementAsync();
    }

    public static async Task WritePointAsync(
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
        await writer.WriteStartElementAsync(null, elementName, GpxXml.NamespaceName);
        await writer.WriteAttributeStringAsync(
            null,
            "lat",
            null,
            point.Coordinate.Latitude.ToString("F7", CultureInfo.InvariantCulture));
        await writer.WriteAttributeStringAsync(
            null,
            "lon",
            null,
            point.Coordinate.Longitude.ToString("F7", CultureInfo.InvariantCulture));

        if (standardChildren is not null)
        {
            await WritePreservedPointContentAsync(
                writer,
                elementName,
                point,
                name,
                description,
                extensions,
                standardChildren,
                cancellationToken);
            await writer.WriteEndElementAsync();
            return;
        }

        await WriteCanonicalPointContentAsync(
            writer,
            point,
            name,
            comment,
            description,
            symbol,
            links,
            extensions,
            cancellationToken);
        await writer.WriteEndElementAsync();
    }

    private static async Task WritePreservedPointContentAsync(
        XmlWriter writer,
        string elementName,
        RoutePoint point,
        string? name,
        string? description,
        IReadOnlyList<XElement>? extensions,
        IReadOnlyList<XElement> standardChildren,
        CancellationToken cancellationToken)
    {
        await WriteElevationAsync(writer, point.ElevationMetres);
        if (elementName == "wpt")
        {
            await GpxPreservedContentWriter.WriteStandardChildrenWithTextReplacementsAsync(
                writer,
                standardChildren,
                cancellationToken,
                new Dictionary<string, string?>
                {
                    ["name"] = name,
                    ["desc"] = description
                },
                "ele");
        }
        else
        {
            await GpxPreservedContentWriter.WriteStandardChildrenAsync(
                writer,
                standardChildren,
                cancellationToken,
                "ele");
        }

        await GpxPreservedContentWriter.WritePreservedExtensionsAsync(
            writer,
            extensions ?? [],
            cancellationToken);
    }

    private static async Task WriteCanonicalPointContentAsync(
        XmlWriter writer,
        RoutePoint point,
        string? name,
        string? comment,
        string? description,
        string? symbol,
        IReadOnlyList<RouteLink>? links,
        IReadOnlyList<XElement>? extensions,
        CancellationToken cancellationToken)
    {
        await WriteElevationAsync(writer, point.ElevationMetres);
        if (point.Time is not null)
        {
            await writer.WriteElementStringAsync(
                null,
                "time",
                GpxXml.NamespaceName,
                XmlConvert.ToString(point.Time.Value));
        }

        await GpxWriterFormatting.WriteOptionalElementAsync(writer, "name", name);
        await GpxWriterFormatting.WriteOptionalElementAsync(writer, "cmt", comment);
        await GpxWriterFormatting.WriteOptionalElementAsync(writer, "desc", description);
        foreach (RouteLink link in links ?? [])
        {
            await WriteLinkAsync(writer, link);
        }

        await GpxWriterFormatting.WriteOptionalElementAsync(writer, "sym", symbol);
        await GpxPreservedContentWriter.WritePreservedExtensionsAsync(
            writer,
            extensions ?? [],
            cancellationToken);
    }

    private static Task WriteElevationAsync(XmlWriter writer, double? elevation) =>
        elevation is null
            ? Task.CompletedTask
            : writer.WriteElementStringAsync(
                null,
                "ele",
                GpxXml.NamespaceName,
                GpxWriterFormatting.FormatElevation(elevation.Value));

    private static async Task WriteAuthorAsync(XmlWriter writer, RouteAuthor author)
    {
        await writer.WriteStartElementAsync(null, "author", GpxXml.NamespaceName);
        await GpxWriterFormatting.WriteOptionalElementAsync(writer, "name", author.Name);
        if (!string.IsNullOrEmpty(author.EmailId) &&
            !string.IsNullOrEmpty(author.EmailDomain))
        {
            await writer.WriteStartElementAsync(null, "email", GpxXml.NamespaceName);
            await writer.WriteAttributeStringAsync(null, "id", null, author.EmailId);
            await writer.WriteAttributeStringAsync(null, "domain", null, author.EmailDomain);
            await writer.WriteEndElementAsync();
        }

        if (author.Link is not null)
        {
            await WriteLinkAsync(writer, author.Link);
        }

        await writer.WriteEndElementAsync();
    }

    private static async Task WriteLinkAsync(XmlWriter writer, RouteLink link)
    {
        await writer.WriteStartElementAsync(null, "link", GpxXml.NamespaceName);
        await writer.WriteAttributeStringAsync(null, "href", null, link.Href);
        await GpxWriterFormatting.WriteOptionalElementAsync(writer, "text", link.Text);
        await GpxWriterFormatting.WriteOptionalElementAsync(writer, "type", link.MimeType);
        await writer.WriteEndElementAsync();
    }
}
