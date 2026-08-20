using RouteTrace.Core.Routes.Documents;
using RouteTrace.Core.Routes.Geometry;
using GpxRoute = RouteTrace.Core.Routes.Documents.Route;

namespace RouteTrace.Core.Editing;

public enum EditableLineKind
{
    TrackSegment,
    Route
}

public readonly record struct EditableLineTarget(
    EditableLineKind Kind,
    int PrimaryIndex,
    int SecondaryIndex = -1)
{
    public static EditableLineTarget TrackSegment(int trackIndex, int segmentIndex) =>
        new(EditableLineKind.TrackSegment, trackIndex, segmentIndex);

    public static EditableLineTarget Route(int routeIndex) =>
        new(EditableLineKind.Route, routeIndex);

    public IReadOnlyList<RoutePoint> GetPoints(RouteDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return Kind switch
        {
            EditableLineKind.TrackSegment => document.Tracks[PrimaryIndex].Segments[SecondaryIndex].Points,
            EditableLineKind.Route => document.Routes[PrimaryIndex].Points,
            _ => throw new InvalidOperationException()
        };
    }

    public IReadOnlyList<int> GetAnchorIndices(RouteDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return Kind switch
        {
            EditableLineKind.TrackSegment => document.Tracks[PrimaryIndex].Segments[SecondaryIndex].AnchorIndices,
            EditableLineKind.Route => document.Routes[PrimaryIndex].AnchorIndices,
            _ => throw new InvalidOperationException()
        };
    }

    public RouteDocument ReplacePoints(
        RouteDocument document,
        IEnumerable<RoutePoint> points,
        IEnumerable<int>? anchorIndices = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(points);
        return Kind switch
        {
            EditableLineKind.TrackSegment => ReplaceSegment(document, points, anchorIndices),
            EditableLineKind.Route => ReplaceRoute(document, points, anchorIndices),
            _ => throw new InvalidOperationException()
        };
    }

    private RouteDocument ReplaceSegment(RouteDocument document, IEnumerable<RoutePoint> points, IEnumerable<int>? anchorIndices)
    {
        Track[] tracks = [.. document.Tracks];
        Track track = tracks[PrimaryIndex];
        TrackSegment[] segments = [.. track.Segments];
        segments[SecondaryIndex] = new TrackSegment(points, anchorIndices);
        tracks[PrimaryIndex] = new Track(track.Name, segments, track.Type);
        return CopyDocument(document, tracks: tracks);
    }

    private RouteDocument ReplaceRoute(RouteDocument document, IEnumerable<RoutePoint> points, IEnumerable<int>? anchorIndices)
    {
        GpxRoute[] routes = [.. document.Routes];
        GpxRoute route = routes[PrimaryIndex];
        routes[PrimaryIndex] = new GpxRoute(route.Name, points, route.UnsupportedExtensionXml, anchorIndices);
        return CopyDocument(document, routes: routes);
    }

    private static RouteDocument CopyDocument(
        RouteDocument document,
        IEnumerable<Track>? tracks = null,
        IEnumerable<GpxRoute>? routes = null) => new(
            tracks ?? document.Tracks,
            routes ?? document.Routes,
            document.Waypoints,
            document.Metadata,
            document.UnsupportedExtensionXml,
            document.UnsupportedExtensionNamespaces);
}
