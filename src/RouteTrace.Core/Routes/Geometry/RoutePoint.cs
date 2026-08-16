namespace RouteTrace.Core.Routes.Geometry;

public sealed record RoutePoint(
    GeoCoordinate Coordinate,
    double? ElevationMetres = null,
    DateTimeOffset? Time = null);
