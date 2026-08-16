namespace RouteTrace.Core.Routes;

public enum WorkspaceNodeKind
{
    Track,
    Segment,
    Route,
    WaypointGroup,
    Waypoint
}

public readonly record struct WorkspaceNode(
    WorkspaceNodeKind Kind,
    int PrimaryIndex = -1,
    int SecondaryIndex = -1);

public sealed record NodePresentationOverride(
    WorkspaceNode Node,
    bool? IsVisible = null,
    string? Colour = null);
