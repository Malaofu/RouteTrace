using System.Xml;
using System.Xml.Linq;
using RouteTrace.Core.Gpx;
using RouteTrace.Core.Gpx.Preservation;
using RouteTrace.Core.Routes.Documents;

namespace RouteTrace.Core.Gpx.Writing;

internal static class GpxPreservedContentWriter
{
    public static async Task WriteRootAttributesAsync(
        XmlWriter writer,
        LazyExtensionXml? preservedXml)
    {
        if (preservedXml is null)
        {
            return;
        }

        foreach ((string prefix, string namespaceName) in preservedXml.RootNamespaceDeclarations)
        {
            await writer.WriteAttributeStringAsync("xmlns", prefix, null, namespaceName);
        }

        foreach ((XName name, string value) in preservedXml.RootAttributes)
        {
            string? prefix = string.IsNullOrEmpty(name.NamespaceName)
                ? null
                : writer.LookupPrefix(name.NamespaceName);
            await writer.WriteAttributeStringAsync(
                prefix,
                name.LocalName,
                name.NamespaceName,
                value);
        }
    }

    public static async Task WriteExtensionXmlAsync(
        XmlWriter writer,
        IReadOnlyList<string> extensions,
        CancellationToken cancellationToken)
    {
        if (extensions.Count == 0)
        {
            return;
        }

        await writer.WriteStartElementAsync(null, "extensions", GpxXml.NamespaceName);
        foreach (string extensionXml in extensions)
        {
            GpxXmlFragmentWriter.WriteExtension(writer, extensionXml, cancellationToken);
        }

        await writer.WriteEndElementAsync();
    }

    public static async Task WritePreservedExtensionsAsync(
        XmlWriter writer,
        IReadOnlyList<XElement> extensions,
        CancellationToken cancellationToken)
    {
        if (extensions.Count == 0)
        {
            return;
        }

        await writer.WriteStartElementAsync(null, "extensions", GpxXml.NamespaceName);
        foreach (XElement extension in extensions)
        {
            GpxXmlFragmentWriter.WriteElement(writer, extension, cancellationToken);
        }

        await writer.WriteEndElementAsync();
    }

    public static Task WriteStandardChildrenAsync(
        XmlWriter writer,
        IReadOnlyList<XElement> children,
        CancellationToken cancellationToken,
        params string[] excludedLocalNames)
    {
        foreach (XElement child in children)
        {
            if (!excludedLocalNames.Contains(child.Name.LocalName, StringComparer.Ordinal))
            {
                GpxXmlFragmentWriter.WriteElement(writer, child, cancellationToken);
            }
        }

        return Task.CompletedTask;
    }

    public static async Task WriteStandardChildrenWithTextReplacementsAsync(
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
            if (excludedLocalNames.Contains(localName, StringComparer.Ordinal))
            {
                continue;
            }

            if (!replacements.TryGetValue(localName, out string? replacement))
            {
                GpxXmlFragmentWriter.WriteElement(writer, child, cancellationToken);
                continue;
            }

            if (written.Add(localName))
            {
                await GpxWriterFormatting.WriteOptionalElementAsync(
                    writer,
                    localName,
                    replacement);
            }
        }

        foreach ((string localName, string? replacement) in replacements)
        {
            if (written.Add(localName))
            {
                await GpxWriterFormatting.WriteOptionalElementAsync(
                    writer,
                    localName,
                    replacement);
            }
        }
    }

    public static async Task WriteDocumentExtensionsAsync(
        XmlWriter writer,
        RouteDocument document,
        LazyExtensionXml? preservedXml,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<XElement>? preservedExtensions =
            preservedXml?.At(GpxExtensionScope.Document);
        IReadOnlyList<string>? extensionXml = preservedXml is null
            ? WithoutOwnedExtensions(document)
            : null;
        if (preservedExtensions is not { Count: > 0 } &&
            extensionXml is not { Count: > 0 })
        {
            return;
        }

        await writer.WriteStartElementAsync(null, "extensions", GpxXml.NamespaceName);
        foreach (XElement extension in preservedExtensions ?? [])
        {
            GpxXmlFragmentWriter.WriteElement(writer, extension, cancellationToken);
        }

        foreach (string extension in extensionXml ?? [])
        {
            GpxXmlFragmentWriter.WriteExtension(writer, extension, cancellationToken);
        }

        await writer.WriteEndElementAsync();
    }

    private static IReadOnlyList<string> WithoutOwnedExtensions(RouteDocument document)
    {
        IEnumerable<string> ownedExtensions =
            (document.Metadata?.UnsupportedExtensionXml ?? [])
            .Concat(document.Routes.SelectMany(route => route.UnsupportedExtensionXml));
        var ownedCounts = ownedExtensions
            .GroupBy(CanonicalExtensionXml, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Count(),
                StringComparer.Ordinal);
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
}
