namespace RouteTrace.Core.Routes;

public sealed class Track
{
    public Track(string? name = null, IEnumerable<TrackSegment>? segments = null, string? type = null)
    {
        Name = name;
        Segments = RouteDocument.Snapshot(segments, nameof(segments));
        Type = type;
    }

    public string? Name { get; }

    /// <summary>
    /// Ordered continuous sections. A boundary between two segments represents
    /// an explicit discontinuity in the track.
    /// </summary>
    public IReadOnlyList<TrackSegment> Segments { get; }

    public string? Type { get; }
}

public sealed class TrackSegment
{
    public TrackSegment(IEnumerable<RoutePoint>? points = null)
    {
        Points = RouteDocument.Snapshot(points, nameof(points));
    }

    public IReadOnlyList<RoutePoint> Points { get; }
}
