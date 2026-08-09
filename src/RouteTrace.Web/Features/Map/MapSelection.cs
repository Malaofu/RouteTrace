namespace RouteTrace.Web.Features.Map;

public readonly record struct MapSelection(
    int? TrackIndex,
    int? SegmentIndex,
    int? RouteIndex = null,
    int? WaypointIndex = null,
    Guid? DocumentId = null,
    bool WholeDocument = false,
    bool WaypointGroup = false)
{
    public static MapSelection None => new(null, null);
}
