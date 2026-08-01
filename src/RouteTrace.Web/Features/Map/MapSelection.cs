namespace RouteTrace.Web.Features.Map;

public readonly record struct MapSelection(int? TrackIndex, int? SegmentIndex)
{
    public static MapSelection None => new(null, null);
}
