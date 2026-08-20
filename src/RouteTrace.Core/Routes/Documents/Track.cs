using RouteTrace.Core.Routes.Geometry;
using RouteTrace.Core.Editing;

namespace RouteTrace.Core.Routes.Documents;

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
    public TrackSegment(IEnumerable<RoutePoint>? points = null, IEnumerable<int>? anchorIndices = null)
    {
        Points = RouteDocument.Snapshot(points, nameof(points));
        AnchorIndices = RouteAnchorSelector.ValidateOrSelect(Points, anchorIndices);
    }

    public IReadOnlyList<RoutePoint> Points { get; }
    public IReadOnlyList<int> AnchorIndices { get; }
}
