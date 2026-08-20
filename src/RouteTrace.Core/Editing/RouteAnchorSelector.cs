using RouteTrace.Core.Routes.Geometry;

namespace RouteTrace.Core.Editing;

public static class RouteAnchorSelector
{
    private const double SimplificationToleranceMetres = 30;
    private const int MaximumAnchors = 48;

    public static IReadOnlyList<int> Select(IReadOnlyList<RoutePoint> points)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (points.Count == 0) return [];
        if (points.Count == 1) return [0];

        var selected = new SortedSet<int> { 0, points.Count - 1 };
        var candidates = new PriorityQueue<SignificantPoint, double>();
        EnqueueSignificantPoint(points, 0, points.Count - 1, candidates);
        while (selected.Count < MaximumAnchors && candidates.TryDequeue(out SignificantPoint candidate, out _))
        {
            if (candidate.DistanceMetres < SimplificationToleranceMetres) break;
            selected.Add(candidate.Index);
            EnqueueSignificantPoint(points, candidate.Start, candidate.Index, candidates);
            EnqueueSignificantPoint(points, candidate.Index, candidate.Finish, candidates);
        }
        return Array.AsReadOnly(selected.ToArray());
    }

    public static IReadOnlyList<int> ValidateOrSelect(
        IReadOnlyList<RoutePoint> points,
        IEnumerable<int>? anchorIndices)
    {
        if (anchorIndices is null) return Select(points);
        int[] indices = anchorIndices.Distinct().Order().ToArray();
        bool valid = points.Count == 0
            ? indices.Length == 0
            : indices.Length > 0 && indices[0] == 0 && indices[^1] == points.Count - 1 &&
              indices.All(index => index >= 0 && index < points.Count);
        if (!valid) throw new ArgumentException("Anchor indices must be ordered point indices including both endpoints.", nameof(anchorIndices));
        return Array.AsReadOnly(indices);
    }

    private static void EnqueueSignificantPoint(
        IReadOnlyList<RoutePoint> points,
        int start,
        int finish,
        PriorityQueue<SignificantPoint, double> candidates)
    {
        if (finish <= start + 1) return;

        double greatestDistance = 0;
        int greatestIndex = -1;
        GeoCoordinate first = points[start].Coordinate;
        GeoCoordinate last = points[finish].Coordinate;
        for (int index = start + 1; index < finish; index++)
        {
            double distance = PerpendicularDistanceMetres(points[index].Coordinate, first, last);
            if (distance <= greatestDistance) continue;
            greatestDistance = distance;
            greatestIndex = index;
        }

        if (greatestIndex >= 0)
            candidates.Enqueue(new(start, finish, greatestIndex, greatestDistance), -greatestDistance);
    }

    private static double PerpendicularDistanceMetres(GeoCoordinate point, GeoCoordinate start, GeoCoordinate finish)
    {
        const double metresPerDegreeLatitude = 111_320;
        double latitudeRadians = (start.Latitude + finish.Latitude + point.Latitude) / 3 * Math.PI / 180;
        double metresPerDegreeLongitude = metresPerDegreeLatitude * Math.Cos(latitudeRadians);
        double x = (point.Longitude - start.Longitude) * metresPerDegreeLongitude;
        double y = (point.Latitude - start.Latitude) * metresPerDegreeLatitude;
        double finishX = (finish.Longitude - start.Longitude) * metresPerDegreeLongitude;
        double finishY = (finish.Latitude - start.Latitude) * metresPerDegreeLatitude;
        double lengthSquared = finishX * finishX + finishY * finishY;
        if (lengthSquared == 0) return Math.Sqrt(x * x + y * y);
        double position = Math.Clamp((x * finishX + y * finishY) / lengthSquared, 0, 1);
        double offsetX = x - position * finishX;
        double offsetY = y - position * finishY;
        return Math.Sqrt(offsetX * offsetX + offsetY * offsetY);
    }

    private readonly record struct SignificantPoint(int Start, int Finish, int Index, double DistanceMetres);
}
