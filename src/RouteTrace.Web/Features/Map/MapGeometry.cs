using RouteTrace.Core.Routes;

namespace RouteTrace.Web.Features.Map;

public sealed record MapGeometry(
    IReadOnlyList<MapTrack> Tracks,
    IReadOnlyList<IReadOnlyList<double[]>> Routes,
    IReadOnlyList<MapWaypoint> Waypoints,
    IReadOnlyList<MapEndpoint> Endpoints)
{
    public static MapGeometry FromDocument(RouteDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        var tracks = new MapTrack[document.Tracks.Count];
        for (int trackIndex = 0; trackIndex < tracks.Length; trackIndex++)
        {
            Track track = document.Tracks[trackIndex];
            var segments = new IReadOnlyList<double[]>[track.Segments.Count];
            for (int segmentIndex = 0; segmentIndex < segments.Length; segmentIndex++)
                segments[segmentIndex] = Coordinates(track.Segments[segmentIndex].Points);
            tracks[trackIndex] = new MapTrack(segments);
        }

        var routes = new IReadOnlyList<double[]>[document.Routes.Count];
        for (int routeIndex = 0; routeIndex < routes.Length; routeIndex++)
            routes[routeIndex] = Coordinates(document.Routes[routeIndex].Points);

        var endpoints = new List<MapEndpoint>();

        for (int index = 0; index < tracks.Length; index++)
            AddTrackEndpoints(endpoints, index, tracks[index]);
        for (int index = 0; index < routes.Length; index++)
            AddEndpoints(endpoints, "route", index, routes[index]);

        var waypoints = new MapWaypoint[document.Waypoints.Count];
        for (int waypointIndex = 0; waypointIndex < waypoints.Length; waypointIndex++)
        {
            Waypoint waypoint = document.Waypoints[waypointIndex];
            waypoints[waypointIndex] = new MapWaypoint(
                Coordinate(waypoint.Point), waypoint.Name, waypoint.Symbol, waypoint.Description ?? waypoint.Comment,
                waypoint.Point.ElevationMetres);
        }

        return new MapGeometry(
            tracks,
            routes,
            waypoints,
            endpoints);
    }

    private static double[][] Coordinates(IReadOnlyList<RoutePoint> points)
    {
        var coordinates = new double[points.Count][];
        for (int index = 0; index < points.Count; index++) coordinates[index] = Coordinate(points[index]);
        return coordinates;
    }

    private static void AddTrackEndpoints(List<MapEndpoint> endpoints, int trackIndex, MapTrack track)
    {
        double[]? first = null;
        double[]? last = null;
        foreach (IReadOnlyList<double[]> segment in track.Segments)
        {
            if (segment.Count == 0) continue;
            first ??= segment[0];
            last = segment[^1];
        }

        if (first is null || last is null) return;
        AddEndpoints(endpoints, "track", trackIndex, first, last);
    }

    private static void AddEndpoints(List<MapEndpoint> endpoints, string ownerKind, int ownerIndex, IReadOnlyList<double[]> points)
    {
        if (points.Count == 0) return;
        AddEndpoints(endpoints, ownerKind, ownerIndex, points[0], points[^1]);
    }

    private static void AddEndpoints(List<MapEndpoint> endpoints, string ownerKind, int ownerIndex, double[] first, double[] last)
    {
        bool overlap = first.SequenceEqual(last);
        endpoints.Add(new(ownerKind, ownerIndex, "start", first, overlap));
        endpoints.Add(new(ownerKind, ownerIndex, "finish", last, overlap));
    }

    private static double[] Coordinate(RoutePoint point) =>
        [point.Coordinate.Longitude, point.Coordinate.Latitude];
}

public sealed record MapTrack(IReadOnlyList<IReadOnlyList<double[]>> Segments);
public sealed record MapWaypoint(
    double[] Coordinate,
    string? Name,
    string? Symbol,
    string? Description,
    double? ElevationMetres);
public sealed record MapEndpoint(string OwnerKind, int OwnerIndex, string EndpointKind, double[] Coordinate, bool Overlap);

public sealed record MapDocumentGeometry(
    Guid Id,
    MapGeometry Geometry,
    string Colour,
    bool IsActive,
    bool IsSelected,
    IReadOnlyList<MapFeaturePresentation> Presentation);

public sealed record MapFeaturePresentation(string Kind, int PrimaryIndex, int SecondaryIndex, bool Visible, string Colour);
