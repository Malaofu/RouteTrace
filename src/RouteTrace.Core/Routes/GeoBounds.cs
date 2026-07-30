namespace RouteTrace.Core.Routes;

/// <summary>An axis-aligned WGS 84 bounding box.</summary>
public readonly record struct GeoBounds
{
    public GeoBounds(double south, double west, double north, double east)
    {
        _ = new GeoCoordinate(south, west);
        _ = new GeoCoordinate(north, east);

        if (south > north)
        {
            throw new ArgumentException("South must not be greater than north.", nameof(south));
        }

        if (west > east)
        {
            throw new ArgumentException("West must not be greater than east.", nameof(west));
        }

        South = south;
        West = west;
        North = north;
        East = east;
    }

    public double South { get; }

    public double West { get; }

    public double North { get; }

    public double East { get; }
}
