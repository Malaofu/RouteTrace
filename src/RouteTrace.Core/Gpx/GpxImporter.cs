using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using RouteTrace.Core.Routes;

namespace RouteTrace.Core.Gpx;

public static class GpxImporter
{
    private static readonly XNamespace Gpx = "http://www.topografix.com/GPX/1/1";

    public static async Task<GpxImportResult> ImportAsync(Stream input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        try
        {
            await using var bufferedInput = new MemoryStream();
            await input.CopyToAsync(bufferedInput, cancellationToken);
            byte[] sourceXml = bufferedInput.ToArray();
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null
            };
            using var parseInput = new MemoryStream(sourceXml, writable: false);
            using XmlReader reader = XmlReader.Create(parseInput, settings);
            return Import(reader, sourceXml, cancellationToken);
        }
        catch (XmlException exception)
        {
            return GpxImportResult.Failure($"The file is not valid XML: {exception.Message}");
        }
        catch (InvalidDataException exception)
        {
            return GpxImportResult.Failure(exception.Message);
        }
    }

    private static GpxImportResult Import(XmlReader reader, byte[] sourceXml, CancellationToken cancellationToken)
    {
        var tracks = new List<Track>();
        var routes = new List<Route>();
        var waypoints = new List<Waypoint>();
        var extensionNamespaces = new HashSet<string>(StringComparer.Ordinal);
        RouteMetadata? metadata = null;
        string? trackName = null;
        List<TrackSegment>? trackSegments = null;
        List<RoutePoint>? segmentPoints = null;
        string? routeName = null;
        List<RoutePoint>? routePoints = null;
        bool foundRoot = false;

        while (reader.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (reader.NodeType == XmlNodeType.Element)
            {
                XName name = Gpx + reader.LocalName;
                if (!foundRoot)
                {
                    if (name != Gpx + "gpx" || reader.NamespaceURI != Gpx.NamespaceName ||
                        reader.GetAttribute("version") != "1.1")
                    {
                        return GpxImportResult.Failure("The file must be a GPX 1.1 document.");
                    }

                    foundRoot = true;
                    continue;
                }

                if (reader.NamespaceURI != Gpx.NamespaceName)
                {
                    continue;
                }

                if (name == Gpx + "metadata")
                {
                    XElement element = ReadElement(reader);
                    metadata = new RouteMetadata(
                        Text(element, "name"), Text(element, "desc"),
                        OptionalTime(element.Element(Gpx + "time")), element.Elements(Gpx + "link").Select(ParseLink));
                    CollectExtensionNamespaces(element, extensionNamespaces);
                }
                else if (name == Gpx + "trk")
                {
                    trackName = null;
                    trackSegments = [];
                }
                else if (name == Gpx + "trkseg")
                {
                    segmentPoints = [];
                }
                else if (name == Gpx + "trkpt")
                {
                    segmentPoints!.Add(ParsePoint(reader, extensionNamespaces, out _));
                }
                else if (name == Gpx + "rte")
                {
                    routeName = null;
                    routePoints = [];
                }
                else if (name == Gpx + "rtept")
                {
                    routePoints!.Add(ParsePoint(reader, extensionNamespaces, out _));
                }
                else if (name == Gpx + "wpt")
                {
                    RoutePoint point = ParsePoint(reader, extensionNamespaces, out string? waypointName);
                    waypoints.Add(new Waypoint(point, waypointName));
                }
                else if (name == Gpx + "name" &&
                         ((trackSegments is not null && segmentPoints is null) || routePoints is not null))
                {
                    string value = ReadElement(reader).Value;
                    if (trackSegments is not null) trackName = value;
                    else if (routePoints is not null) routeName = value;
                }
                else if (name == Gpx + "extensions")
                {
                    CollectExtensionNamespaces(reader, extensionNamespaces);
                }
            }
            else if (reader.NodeType == XmlNodeType.EndElement && reader.NamespaceURI == Gpx.NamespaceName)
            {
                if (reader.LocalName == "trkseg")
                {
                    trackSegments!.Add(new TrackSegment(segmentPoints));
                    segmentPoints = null;
                }
                else if (reader.LocalName == "trk")
                {
                    tracks.Add(new Track(trackName, trackSegments));
                    trackSegments = null;
                }
                else if (reader.LocalName == "rte")
                {
                    routes.Add(new Route(routeName, routePoints));
                    routePoints = null;
                }
            }
        }

        return foundRoot
            ? GpxImportResult.Success(new RouteDocument(
                tracks, routes, waypoints, metadata, new LazyExtensionXml(sourceXml),
                extensionNamespaces.Order(StringComparer.Ordinal).ToArray()))
            : GpxImportResult.Failure("The file must be a GPX 1.1 document.");
    }

    private static void CollectExtensionNamespaces(
        XElement parent,
        ISet<string> extensionNamespaces)
    {
        IEnumerable<XElement> extensionElements = parent.Name == Gpx + "extensions"
            ? parent.Elements()
            : parent.Descendants(Gpx + "extensions").Elements();
        foreach (XElement element in extensionElements.Where(element => element.Name.Namespace != Gpx))
        {
            if (!string.IsNullOrWhiteSpace(element.Name.NamespaceName))
            {
                extensionNamespaces.Add(element.Name.NamespaceName);
            }
        }
    }

    private static void CollectExtensionNamespaces(
        XmlReader reader,
        ISet<string> extensionNamespaces)
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

            if (reader.NodeType != XmlNodeType.Element || reader.Depth != containerDepth + 1 ||
                reader.NamespaceURI == Gpx.NamespaceName)
            {
                continue;
            }

            string namespaceName = reader.NamespaceURI;
            reader.Skip();
            advance = false;
            if (!string.IsNullOrWhiteSpace(namespaceName))
            {
                extensionNamespaces.Add(namespaceName);
            }
        }
    }

    private static XElement ReadElement(XmlReader reader) => (XElement)XNode.ReadFrom(reader);

    private static RoutePoint ParsePoint(
        XmlReader reader,
        ISet<string> extensionNamespaces,
        out string? name)
    {
        double latitude = RequiredCoordinate(reader.GetAttribute("lat"), "lat", -90, 90);
        double longitude = RequiredCoordinate(reader.GetAttribute("lon"), "lon", -180, 180);
        double? elevation = null;
        DateTimeOffset? time = null;
        name = null;
        int pointDepth = reader.Depth;
        if (reader.IsEmptyElement)
        {
            return new RoutePoint(new GeoCoordinate(latitude, longitude));
        }

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == pointDepth)
            {
                break;
            }

            if (reader.NodeType != XmlNodeType.Element || reader.NamespaceURI != Gpx.NamespaceName)
            {
                continue;
            }

            if (reader.LocalName == "ele")
            {
                elevation = OptionalNumber(reader.ReadString(), "elevation");
            }
            else if (reader.LocalName == "time")
            {
                time = OptionalTime(reader.ReadString());
            }
            else if (reader.LocalName == "name")
            {
                name = reader.ReadString();
            }
            else if (reader.LocalName == "extensions")
            {
                CollectExtensionNamespaces(reader, extensionNamespaces);
            }
        }

        return new RoutePoint(new GeoCoordinate(latitude, longitude), elevation, time);
    }

    private static RoutePoint ParsePoint(XElement element)
    {
        double latitude = RequiredCoordinate(element, "lat", -90, 90);
        double longitude = RequiredCoordinate(element, "lon", -180, 180);
        return new RoutePoint(
            new GeoCoordinate(latitude, longitude),
            OptionalNumber(element.Element(Gpx + "ele"), "elevation"),
            OptionalTime(element.Element(Gpx + "time")));
    }

    private static double RequiredCoordinate(XElement element, string attributeName, double minimum, double maximum)
        => RequiredCoordinate((string?)element.Attribute(attributeName), attributeName, minimum, maximum);

    private static double RequiredCoordinate(string? text, string attributeName, double minimum, double maximum)
    {
        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value) ||
            !double.IsFinite(value) || value < minimum || value > maximum)
        {
            throw new InvalidDataException($"A GPX point has an invalid {attributeName} coordinate: '{text ?? "missing"}'.");
        }

        return value;
    }

    private static double? OptionalNumber(XElement? element, string field)
    {
        if (element is null)
        {
            return null;
        }

        return OptionalNumber(element.Value, field);
    }

    private static double OptionalNumber(string text, string field)
    {
        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value) || !double.IsFinite(value))
        {
            throw new InvalidDataException($"A GPX point has an invalid {field} value: '{text}'.");
        }

        return value;
    }

    private static DateTimeOffset? OptionalTime(XElement? element)
    {
        if (element is null)
        {
            return null;
        }

        return OptionalTime(element.Value);
    }

    private static DateTimeOffset OptionalTime(string text)
    {
        try
        {
            return XmlConvert.ToDateTimeOffset(text);
        }
        catch (FormatException)
        {
            throw new InvalidDataException($"The GPX file has an invalid timestamp: '{text}'.");
        }
    }

    private static string? Text(XElement parent, string localName) =>
        parent.Element(Gpx + localName)?.Value;

    private static RouteLink ParseLink(XElement element)
    {
        string? href = (string?)element.Attribute("href");
        if (string.IsNullOrWhiteSpace(href))
        {
            throw new InvalidDataException("A GPX metadata link is missing its href attribute.");
        }

        return new RouteLink(href, Text(element, "text"), Text(element, "type"));
    }
}
