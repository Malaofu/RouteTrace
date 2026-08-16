using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using RouteTrace.Core.Gpx;
using RouteTrace.Core.Gpx.Preservation;
using RouteTrace.Core.Routes.Documents;
using RouteTrace.Core.Routes.Geometry;

namespace RouteTrace.Core.Gpx.Parsing;

internal static class GpxElementParser
{
    public static XElement ReadElement(XmlReader reader) => (XElement)XNode.ReadFrom(reader);

    public static RouteMetadata ParseMetadata(
        XElement element,
        LazyExtensionXml preservedXml) =>
        new(
            Text(element, "name"),
            Text(element, "desc"),
            OptionalTime(element.Element(GpxXml.Namespace + "time")),
            element.Elements(GpxXml.Namespace + "link").Select(ParseLink).ToArray(),
            ParseAuthor(element.Element(GpxXml.Namespace + "author")),
            preservedXml.StringViewAt(GpxExtensionScope.Metadata));

    public static Waypoint ParseWaypoint(XElement element) =>
        new(
            ParsePoint(element),
            Text(element, "name"),
            Text(element, "cmt"),
            Text(element, "desc"),
            Text(element, "sym"),
            element.Elements(GpxXml.Namespace + "link").Select(ParseLink).ToArray());

    public static RoutePoint ParsePoint(
        XmlReader reader,
        ISet<string> extensionNamespaces)
    {
        double latitude = RequiredCoordinate(reader.GetAttribute("lat"), "lat", -90, 90);
        double longitude = RequiredCoordinate(reader.GetAttribute("lon"), "lon", -180, 180);
        double? elevation = null;
        DateTimeOffset? time = null;
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

            if (reader.NodeType != XmlNodeType.Element ||
                reader.NamespaceURI != GpxXml.NamespaceName)
            {
                continue;
            }

            switch (reader.LocalName)
            {
                case "ele":
                    elevation = OptionalNumber(reader.ReadString(), "elevation");
                    break;
                case "time":
                    time = OptionalTime(reader.ReadString());
                    break;
                case "extensions":
                    GpxExtensionNamespaceCollector.Collect(reader, extensionNamespaces);
                    break;
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
            OptionalNumber(element.Element(GpxXml.Namespace + "ele"), "elevation"),
            OptionalTime(element.Element(GpxXml.Namespace + "time")));
    }

    private static double RequiredCoordinate(
        XElement element,
        string attributeName,
        double minimum,
        double maximum) =>
        RequiredCoordinate(
            (string?)element.Attribute(attributeName),
            attributeName,
            minimum,
            maximum);

    private static double RequiredCoordinate(
        string? text,
        string attributeName,
        double minimum,
        double maximum)
    {
        if (!double.TryParse(
                text,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double value) ||
            !double.IsFinite(value) ||
            value < minimum ||
            value > maximum)
        {
            throw new InvalidDataException(
                $"A GPX point has an invalid {attributeName} coordinate: '{text ?? "missing"}'.");
        }

        return value;
    }

    private static double? OptionalNumber(XElement? element, string field) =>
        element is null ? null : OptionalNumber(element.Value, field);

    private static double OptionalNumber(string text, string field)
    {
        if (!double.TryParse(
                text,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double value) ||
            !double.IsFinite(value))
        {
            throw new InvalidDataException(
                $"A GPX point has an invalid {field} value: '{text}'.");
        }

        return value;
    }

    private static DateTimeOffset? OptionalTime(XElement? element) =>
        element is null ? null : OptionalTime(element.Value);

    private static DateTimeOffset OptionalTime(string text)
    {
        try
        {
            return XmlConvert.ToDateTimeOffset(text);
        }
        catch (FormatException)
        {
            throw new InvalidDataException(
                $"The GPX file has an invalid timestamp: '{text}'.");
        }
    }

    private static string? Text(XElement parent, string localName) =>
        parent.Element(GpxXml.Namespace + localName)?.Value;

    private static RouteLink ParseLink(XElement element)
    {
        string? href = (string?)element.Attribute("href");
        if (string.IsNullOrWhiteSpace(href))
        {
            throw new InvalidDataException(
                "A GPX metadata link is missing its href attribute.");
        }

        return new RouteLink(href, Text(element, "text"), Text(element, "type"));
    }

    private static RouteAuthor? ParseAuthor(XElement? element)
    {
        if (element is null)
        {
            return null;
        }

        XElement? email = element.Element(GpxXml.Namespace + "email");
        XElement? link = element.Element(GpxXml.Namespace + "link");
        return new RouteAuthor(
            Text(element, "name"),
            (string?)email?.Attribute("id"),
            (string?)email?.Attribute("domain"),
            link is null ? null : ParseLink(link));
    }
}
