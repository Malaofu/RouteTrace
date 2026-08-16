using System.Xml;
using System.Xml.Linq;
using RouteTrace.Core.Gpx;
using RouteTrace.Core.Gpx.Preservation;
using RouteTrace.Core.Routes.Documents;
using RouteTrace.Core.Routes.Geometry;

namespace RouteTrace.Core.Gpx.Parsing;

internal sealed class GpxStreamParser(
    XmlReader reader,
    LazyExtensionXml preservedXml,
    CancellationToken cancellationToken)
{
    private readonly List<Track> tracks = [];
    private readonly List<Route> routes = [];
    private readonly List<Waypoint> waypoints = [];
    private readonly HashSet<string> extensionNamespaces = new(StringComparer.Ordinal);
    private RouteMetadata? metadata;
    private string? trackName;
    private string? trackType;
    private List<TrackSegment>? trackSegments;
    private List<RoutePoint>? segmentPoints;
    private string? routeName;
    private List<RoutePoint>? routePoints;
    private int routeIndex = -1;
    private bool foundRoot;

    public GpxImportResult Parse()
    {
        while (reader.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();
            switch (reader.NodeType)
            {
                case XmlNodeType.Element:
                    HandleStartElement();
                    break;
                case XmlNodeType.EndElement when reader.NamespaceURI == GpxXml.NamespaceName:
                    HandleEndElement();
                    break;
            }
        }

        return foundRoot
            ? GpxImportResult.Success(new RouteDocument(
                tracks,
                routes,
                waypoints,
                metadata,
                preservedXml,
                extensionNamespaces.Order(StringComparer.Ordinal).ToArray()))
            : GpxImportResult.Failure("The file must be a GPX 1.1 document.");
    }

    private void HandleStartElement()
    {
        if (!foundRoot)
        {
            ValidateRoot();
            return;
        }

        if (reader.NamespaceURI != GpxXml.NamespaceName)
        {
            return;
        }

        switch (reader.LocalName)
        {
            case "metadata":
                ReadMetadata();
                break;
            case "trk":
                StartTrack();
                break;
            case "trkseg":
                StartTrackSegment();
                break;
            case "trkpt":
                ReadTrackPoint();
                break;
            case "rte":
                StartRoute();
                break;
            case "rtept":
                ReadRoutePoint();
                break;
            case "wpt":
                ReadWaypoint();
                break;
            case "name" when IsTrackHeader || routePoints is not null:
                ReadName();
                break;
            case "type" when IsTrackHeader:
                trackType = GpxElementParser.ReadElement(reader).Value;
                break;
            case "extensions":
                GpxExtensionNamespaceCollector.Collect(reader, extensionNamespaces);
                break;
        }
    }

    private void HandleEndElement()
    {
        switch (reader.LocalName)
        {
            case "trkseg":
                FinishTrackSegment();
                break;
            case "trk":
                FinishTrack();
                break;
            case "rte":
                FinishRoute();
                break;
        }
    }

    private bool IsTrackHeader => trackSegments is not null && segmentPoints is null;

    private void ValidateRoot()
    {
        if (reader.LocalName != "gpx" ||
            reader.NamespaceURI != GpxXml.NamespaceName ||
            reader.GetAttribute("version") != "1.1")
        {
            throw new InvalidDataException("The file must be a GPX 1.1 document.");
        }

        foundRoot = true;
    }

    private void ReadMetadata()
    {
        XElement element = GpxElementParser.ReadElement(reader);
        metadata = GpxElementParser.ParseMetadata(element, preservedXml);
        GpxExtensionNamespaceCollector.Collect(element, extensionNamespaces);
    }

    private void StartTrack()
    {
        trackName = null;
        trackType = null;
        trackSegments = [];
        if (reader.IsEmptyElement)
        {
            tracks.Add(new Track());
            trackSegments = null;
        }
    }

    private void StartTrackSegment()
    {
        if (trackSegments is null)
        {
            throw new InvalidDataException("A GPX track segment must belong to a track.");
        }

        if (reader.IsEmptyElement)
        {
            trackSegments.Add(new TrackSegment());
            return;
        }

        segmentPoints = [];
    }

    private void ReadTrackPoint()
    {
        if (segmentPoints is null)
        {
            throw new InvalidDataException("A GPX track point must belong to a track segment.");
        }

        segmentPoints.Add(GpxElementParser.ParsePoint(reader, extensionNamespaces));
    }

    private void StartRoute()
    {
        routeName = null;
        routePoints = [];
        routeIndex = routes.Count;
        if (reader.IsEmptyElement)
        {
            FinishRoute();
        }
    }

    private void ReadRoutePoint()
    {
        if (routePoints is null)
        {
            throw new InvalidDataException("A GPX route point must belong to a route.");
        }

        routePoints.Add(GpxElementParser.ParsePoint(reader, extensionNamespaces));
    }

    private void ReadWaypoint()
    {
        XElement element = GpxElementParser.ReadElement(reader);
        waypoints.Add(GpxElementParser.ParseWaypoint(element));
        GpxExtensionNamespaceCollector.Collect(element, extensionNamespaces);
    }

    private void ReadName()
    {
        string value = GpxElementParser.ReadElement(reader).Value;
        if (trackSegments is not null)
        {
            trackName = value;
        }
        else if (routePoints is not null)
        {
            routeName = value;
        }
    }

    private void FinishTrackSegment()
    {
        if (trackSegments is null || segmentPoints is null)
        {
            throw new InvalidDataException(
                "A GPX track segment is not correctly nested inside a track.");
        }

        trackSegments.Add(new TrackSegment(segmentPoints));
        segmentPoints = null;
    }

    private void FinishTrack()
    {
        if (trackSegments is null)
        {
            throw new InvalidDataException("A GPX track is not correctly formed.");
        }

        tracks.Add(new Track(trackName, trackSegments, trackType));
        trackSegments = null;
    }

    private void FinishRoute()
    {
        if (routePoints is null)
        {
            throw new InvalidDataException("A GPX route is not correctly formed.");
        }

        routes.Add(new Route(
            routeName,
            routePoints,
            preservedXml.StringViewAt(GpxExtensionScope.Route, routeIndex)));
        routePoints = null;
        routeIndex = -1;
    }
}
