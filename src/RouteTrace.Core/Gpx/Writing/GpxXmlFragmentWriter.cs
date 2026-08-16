using System.Xml;
using System.Xml.Linq;

namespace RouteTrace.Core.Gpx.Writing;

internal static class GpxXmlFragmentWriter
{
    public static void WriteExtension(
        XmlWriter writer,
        string extensionXml,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        XElement extension = XElement.Parse(extensionXml, LoadOptions.None);
        WriteElement(writer, extension, cancellationToken);
    }

    public static void WriteElement(
        XmlWriter writer,
        XElement element,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string? prefix = string.IsNullOrEmpty(element.Name.NamespaceName)
            ? null
            : writer.LookupPrefix(element.Name.NamespaceName) ??
              DeclaredPrefix(element, element.Name.NamespaceName);
        writer.WriteStartElement(prefix, element.Name.LocalName, element.Name.NamespaceName);

        foreach (XAttribute declaration in element.Attributes().Where(
                     attribute => attribute.IsNamespaceDeclaration))
        {
            string declaredPrefix = declaration.Name.LocalName == "xmlns"
                ? string.Empty
                : declaration.Name.LocalName;
            if (writer.LookupPrefix(declaration.Value) != declaredPrefix)
            {
                writer.WriteAttributeString(
                    "xmlns",
                    declaredPrefix,
                    null,
                    declaration.Value);
            }
        }

        foreach (XAttribute attribute in element.Attributes().Where(
                     attribute => !attribute.IsNamespaceDeclaration))
        {
            string? attributePrefix = string.IsNullOrEmpty(attribute.Name.NamespaceName)
                ? null
                : writer.LookupPrefix(attribute.Name.NamespaceName) ??
                  DeclaredPrefix(element, attribute.Name.NamespaceName);
            writer.WriteAttributeString(
                attributePrefix,
                attribute.Name.LocalName,
                attribute.Name.NamespaceName,
                attribute.Value);
        }

        foreach (XNode node in element.Nodes())
        {
            if (node is XElement child)
            {
                WriteElement(writer, child, cancellationToken);
            }
            else
            {
                node.WriteTo(writer);
            }
        }

        writer.WriteEndElement();
    }

    private static string? DeclaredPrefix(XElement element, string namespaceName)
    {
        XAttribute? declaration = element.Attributes().FirstOrDefault(
            attribute =>
                attribute.IsNamespaceDeclaration &&
                attribute.Value == namespaceName);
        return declaration?.Name.LocalName == "xmlns"
            ? string.Empty
            : declaration?.Name.LocalName;
    }
}
