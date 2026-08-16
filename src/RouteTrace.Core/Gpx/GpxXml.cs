using System.Xml.Linq;

namespace RouteTrace.Core.Gpx;

internal static class GpxXml
{
    public const string NamespaceName = "http://www.topografix.com/GPX/1/1";
    public static readonly XNamespace Namespace = NamespaceName;
}
