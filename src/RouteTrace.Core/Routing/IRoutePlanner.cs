using RouteTrace.Core.Routes.Geometry;

namespace RouteTrace.Core.Routing;

public interface IRoutePlanner
{
    Task<RoutePlanResult> PlanAsync(RoutePlanRequest request, CancellationToken cancellationToken = default);
}

public sealed record RoutePlanRequest(
    IReadOnlyList<GeoCoordinate> Anchors,
    BicycleRoutingProfile Profile = BicycleRoutingProfile.Cycling)
{
    public RoutePlanRequest(
        IEnumerable<GeoCoordinate> anchors,
        BicycleRoutingProfile profile = BicycleRoutingProfile.Cycling)
        : this(Array.AsReadOnly(anchors.ToArray()), profile)
    {
    }
}

public enum BicycleRoutingProfile
{
    Cycling,
    Gravel,
    MountainBike
}

public enum RoutePlanStatus
{
    Success,
    NoRoute,
    Failure
}

public sealed record RoutePlanResult(
    RoutePlanStatus Status,
    IReadOnlyList<RoutePoint> Geometry,
    string? Message = null)
{
    public static RoutePlanResult Success(IEnumerable<RoutePoint> geometry) =>
        new(RoutePlanStatus.Success, Array.AsReadOnly(geometry.ToArray()));

    public static RoutePlanResult NoRoute(string? message = null) =>
        new(RoutePlanStatus.NoRoute, [], message ?? "No bicycle route was found between these anchors.");

    public static RoutePlanResult Failure(string? message = null) =>
        new(RoutePlanStatus.Failure, [], message ?? "The routing provider could not calculate the route.");
}
