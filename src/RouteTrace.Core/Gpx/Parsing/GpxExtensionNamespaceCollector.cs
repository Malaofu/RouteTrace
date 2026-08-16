using System.Xml;
using System.Xml.Linq;
using RouteTrace.Core.Gpx;

namespace RouteTrace.Core.Gpx.Parsing;

internal static class GpxExtensionNamespaceCollector
{
    public static void Collect(XElement parent, ISet<string> extensionNamespaces)
    {
        IEnumerable<XElement> extensionElements = parent.Name == GpxXml.Namespace + "extensions"
            ? parent.Elements()
            : parent.Descendants(GpxXml.Namespace + "extensions").Elements();
        foreach (XElement element in extensionElements.Where(
                     element => element.Name.Namespace != GpxXml.Namespace))
        {
            Add(element.Name.NamespaceName, extensionNamespaces);
        }
    }

    public static void Collect(XmlReader reader, ISet<string> extensionNamespaces)
    {
        int containerDepth = reader.Depth;
        bool advance = true;
        while (true)
        {
            if (advance && !reader.Read())
            {
                return;
            }

            advance = true;
            if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == containerDepth)
            {
                return;
            }

            if (reader.NodeType != XmlNodeType.Element ||
                reader.Depth != containerDepth + 1 ||
                reader.NamespaceURI == GpxXml.NamespaceName)
            {
                continue;
            }

            string namespaceName = reader.NamespaceURI;
            reader.Skip();
            advance = false;
            Add(namespaceName, extensionNamespaces);
        }
    }

    private static void Add(string namespaceName, ISet<string> extensionNamespaces)
    {
        if (!string.IsNullOrWhiteSpace(namespaceName))
        {
            extensionNamespaces.Add(namespaceName);
        }
    }
}
