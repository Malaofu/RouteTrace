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
        MapTrack[] tracks = document.Tracks.Select(track => new MapTrack(
                track.Segments.Select(segment =>
                    (IReadOnlyList<double[]>)segment.Points.Select(Coordinate).ToArray()).ToArray())).ToArray();
        IReadOnlyList<double[]>[] routes = document.Routes.Select(route =>
            (IReadOnlyList<double[]>)route.Points.Select(Coordinate).ToArray()).ToArray();
        var endpoints = new List<MapEndpoint>();

        for (int index = 0; index < tracks.Length; index++)
        {
            double[][] populated = tracks[index].Segments.Where(segment => segment.Count > 0).SelectMany(segment => segment).ToArray();
            AddEndpoints(endpoints, "track", index, populated);
        }
        for (int index = 0; index < routes.Length; index++)
            AddEndpoints(endpoints, "route", index, routes[index]);

        return new MapGeometry(
            tracks,
            routes,
            document.Waypoints.Select(waypoint => new MapWaypoint(
                Coordinate(waypoint.Point), waypoint.Name, waypoint.Symbol, waypoint.Description ?? waypoint.Comment,
                waypoint.Point.ElevationMetres)).ToArray(),
            endpoints);
    }

    private static void AddEndpoints(List<MapEndpoint> endpoints, string ownerKind, int ownerIndex, IReadOnlyList<double[]> points)
    {
        if (points.Count == 0) return;
        bool overlap = points[0].SequenceEqual(points[^1]);
        endpoints.Add(new(ownerKind, ownerIndex, "start", points[0], overlap));
        endpoints.Add(new(ownerKind, ownerIndex, "finish", points[^1], overlap));
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
