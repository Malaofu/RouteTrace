using RouteTrace.Core.Routes;

namespace RouteTrace.Web.Features.Map;

public sealed record MapGeometry(
    IReadOnlyList<MapTrack> Tracks,
    IReadOnlyList<IReadOnlyList<double[]>> Routes,
    IReadOnlyList<double[]> Waypoints)
{
    public static MapGeometry FromDocument(RouteDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return new MapGeometry(
            document.Tracks.Select(track => new MapTrack(
                track.Segments.Select(segment =>
                    (IReadOnlyList<double[]>)segment.Points.Select(Coordinate).ToArray()).ToArray())).ToArray(),
            document.Routes.Select(route =>
                (IReadOnlyList<double[]>)route.Points.Select(Coordinate).ToArray()).ToArray(),
            document.Waypoints.Select(waypoint => Coordinate(waypoint.Point)).ToArray());
    }

    private static double[] Coordinate(RoutePoint point) =>
        [point.Coordinate.Longitude, point.Coordinate.Latitude];
}

public sealed record MapTrack(IReadOnlyList<IReadOnlyList<double[]>> Segments);

public sealed record MapDocumentGeometry(
    Guid Id,
    MapGeometry Geometry,
    string Colour,
    bool IsActive,
    bool IsSelected);
