namespace RouteTrace.Core.Routes;

public static class RouteStatisticsCalculator
{
    private const double EarthRadiusMetres = 6_371_008.8;

    public static RouteStatistics Calculate(RouteDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        SegmentStatistics[] segments = document.Tracks
            .SelectMany((track, trackIndex) => track.Segments.Select((segment, segmentIndex) =>
                new SegmentStatistics(trackIndex, segmentIndex, segment.Points.Count, Distance(segment.Points))))
            .ToArray();
        IReadOnlyList<RoutePoint> trackPoints = document.Tracks
            .SelectMany(track => track.Segments)
            .SelectMany(segment => segment.Points)
            .ToArray();

        return new RouteStatistics(
            document.Tracks.Count,
            document.Tracks.Sum(track => track.Segments.Count),
            trackPoints.Count,
            document.Routes.Count,
            document.Routes.Sum(route => route.Points.Count),
            document.Waypoints.Count,
            segments,
            segments.Sum(segment => segment.DistanceMetres),
            Elevation(document.Tracks.SelectMany(track => track.Segments).ToArray()),
            Time(trackPoints),
            document.UnsupportedExtensionNamespaces);
    }

    private static double Distance(IReadOnlyList<RoutePoint> points)
    {
        double distance = 0;
        for (int index = 1; index < points.Count; index++)
        {
            distance += Haversine(points[index - 1].Coordinate, points[index].Coordinate);
        }

        return distance;
    }

    private static double Haversine(GeoCoordinate first, GeoCoordinate second)
    {
        double latitude1 = DegreesToRadians(first.Latitude);
        double latitude2 = DegreesToRadians(second.Latitude);
        double latitudeDelta = latitude2 - latitude1;
        double longitudeDelta = DegreesToRadians(second.Longitude - first.Longitude);
        double a = Math.Pow(Math.Sin(latitudeDelta / 2), 2) +
            Math.Cos(latitude1) * Math.Cos(latitude2) * Math.Pow(Math.Sin(longitudeDelta / 2), 2);
        return 2 * EarthRadiusMetres * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static ElevationStatistics? Elevation(IReadOnlyList<TrackSegment> segments)
    {
        IReadOnlyList<RoutePoint> points = segments.SelectMany(segment => segment.Points).ToArray();
        double[] values = points.Where(point => point.ElevationMetres.HasValue)
            .Select(point => point.ElevationMetres.GetValueOrDefault()).ToArray();
        if (values.Length == 0)
        {
            return null;
        }

        bool complete = values.Length == points.Count;
        double? ascent = null;
        double? descent = null;
        if (complete && values.Length > 1)
        {
            ascent = 0;
            descent = 0;
            foreach (TrackSegment segment in segments)
            {
                for (int index = 1; index < segment.Points.Count; index++)
                {
                    double change = segment.Points[index].ElevationMetres.GetValueOrDefault() -
                        segment.Points[index - 1].ElevationMetres.GetValueOrDefault();
                    if (change > 0) ascent += change;
                    if (change < 0) descent -= change;
                }
            }
        }

        return new ElevationStatistics(values.Min(), values.Max(), ascent, descent, complete);
    }

    private static TimeStatistics? Time(IReadOnlyList<RoutePoint> points)
    {
        DateTimeOffset[] values = points.Where(point => point.Time.HasValue)
            .Select(point => point.Time.GetValueOrDefault()).Order().ToArray();
        return values.Length == 0
            ? null
            : new TimeStatistics(values[0], values[^1], values.Length > 1 ? values[^1] - values[0] : null);
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180;
}

public sealed record RouteStatistics(
    int TrackCount,
    int SegmentCount,
    int TrackPointCount,
    int RouteCount,
    int RoutePointCount,
    int WaypointCount,
    IReadOnlyList<SegmentStatistics> Segments,
    double TotalTrackDistanceMetres,
    ElevationStatistics? Elevation,
    TimeStatistics? Time,
    IReadOnlyList<string> ExtensionNamespaces);

public sealed record SegmentStatistics(int TrackIndex, int SegmentIndex, int PointCount, double DistanceMetres);

public sealed record ElevationStatistics(
    double MinimumMetres,
    double MaximumMetres,
    double? AscentMetres,
    double? DescentMetres,
    bool IsComplete);

public sealed record TimeStatistics(DateTimeOffset Start, DateTimeOffset End, TimeSpan? Duration);
