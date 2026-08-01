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
            var settings = new XmlReaderSettings
            {
                Async = true,
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null
            };
            using XmlReader reader = XmlReader.Create(input, settings);
            XDocument xml = await XDocument.LoadAsync(reader, LoadOptions.PreserveWhitespace, cancellationToken);
            return Import(xml);
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

    private static GpxImportResult Import(XDocument xml)
    {
        XElement? root = xml.Root;
        if (root?.Name != Gpx + "gpx" || (string?)root.Attribute("version") != "1.1")
        {
            return GpxImportResult.Failure("The file must be a GPX 1.1 document.");
        }

        try
        {
            XElement? metadataElement = root.Element(Gpx + "metadata");
            RouteMetadata? metadata = metadataElement is null
                ? null
                : new RouteMetadata(
                    Text(metadataElement, "name"),
                    Text(metadataElement, "desc"),
                    OptionalTime(metadataElement.Element(Gpx + "time")),
                    metadataElement.Elements(Gpx + "link").Select(ParseLink));

            Track[] tracks = root.Elements(Gpx + "trk")
                .Select(track => new Track(
                    Text(track, "name"),
                    track.Elements(Gpx + "trkseg")
                        .Select(segment => new TrackSegment(segment.Elements(Gpx + "trkpt").Select(ParsePoint)))))
                .ToArray();
            Route[] routes = root.Elements(Gpx + "rte")
                .Select(route => new Route(Text(route, "name"), route.Elements(Gpx + "rtept").Select(ParsePoint)))
                .ToArray();
            Waypoint[] waypoints = root.Elements(Gpx + "wpt")
                .Select(point => new Waypoint(ParsePoint(point), Text(point, "name")))
                .ToArray();
            string[] extensions = root.Descendants(Gpx + "extensions")
                .Elements()
                .Where(element => element.Name.Namespace != Gpx)
                .Select(element => element.ToString(SaveOptions.DisableFormatting))
                .ToArray();

            return GpxImportResult.Success(new RouteDocument(tracks, routes, waypoints, metadata, extensions));
        }
        catch (InvalidDataException exception)
        {
            return GpxImportResult.Failure(exception.Message);
        }
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
    {
        string? text = (string?)element.Attribute(attributeName);
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

        if (!double.TryParse(element.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double value) || !double.IsFinite(value))
        {
            throw new InvalidDataException($"A GPX point has an invalid {field} value: '{element.Value}'.");
        }

        return value;
    }

    private static DateTimeOffset? OptionalTime(XElement? element)
    {
        if (element is null)
        {
            return null;
        }

        if (!DateTimeOffset.TryParse(element.Value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTimeOffset value))
        {
            throw new InvalidDataException($"The GPX file has an invalid timestamp: '{element.Value}'.");
        }

        return value;
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
