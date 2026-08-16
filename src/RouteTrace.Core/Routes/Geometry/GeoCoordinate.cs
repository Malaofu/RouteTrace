namespace RouteTrace.Core.Routes.Geometry;

/// <summary>A WGS 84 geographic coordinate.</summary>
public readonly record struct GeoCoordinate
{
    public GeoCoordinate(double latitude, double longitude)
    {
        if (!double.IsFinite(latitude) || latitude is < -90 or > 90)
        {
            throw new ArgumentOutOfRangeException(
                nameof(latitude),
                latitude,
                "Latitude must be finite and between -90 and 90 degrees.");
        }

        if (!double.IsFinite(longitude) || longitude is < -180 or > 180)
        {
            throw new ArgumentOutOfRangeException(
                nameof(longitude),
                longitude,
                "Longitude must be finite and between -180 and 180 degrees.");
        }

        Latitude = latitude;
        Longitude = longitude;
    }

    public double Latitude { get; }

    public double Longitude { get; }
}
