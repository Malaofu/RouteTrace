using RouteTrace.Core.Routes.Geometry;

namespace RouteTrace.Core.Routes.Documents;

public sealed record Waypoint(
    RoutePoint Point,
    string? Name = null,
    string? Comment = null,
    string? Description = null,
    string? Symbol = null,
    IReadOnlyList<RouteLink>? Links = null)
{
    public RoutePoint Point { get; } =
        Point ?? throw new ArgumentNullException(nameof(Point));

    public IReadOnlyList<RouteLink> Links { get; } = Links ?? [];
}
