namespace RouteTrace.Core.Routes;

public sealed record RoutePoint(
    GeoCoordinate Coordinate,
    double? ElevationMetres = null,
    DateTimeOffset? Time = null);
