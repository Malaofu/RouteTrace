using RouteTrace.Core.Routes.Documents;
using RouteTrace.Core.Routes.Workspaces;
using RouteTrace.Web.Features.Map;

namespace RouteTrace.Web.Features.Workspaces;

public sealed record DocumentTreeTarget(
    WorkspaceDocument Document,
    WorkspaceNode? Node,
    string Name,
    MapSelection Selection);

public readonly record struct DocumentTreeNodeIdentity(
    Guid DocumentId,
    WorkspaceNode? Node);

public readonly record struct DocumentTreeSelectionRequest(
    DocumentTreeTarget Target,
    bool Additive);

public readonly record struct DocumentTreeActionRequest(
    DocumentTreeTarget Target,
    double ClientX,
    double ClientY);

internal static class DocumentTreeTargetFactory
{
    public static DocumentTreeTarget Create(WorkspaceDocument document, WorkspaceNode? node) => node switch
    {
        null => new(
            document,
            null,
            DocumentName(document),
            new(null, null, DocumentId: document.Id, WholeDocument: true)),
        { Kind: WorkspaceNodeKind.Track } value => new(
            document,
            value,
            document.Document.Tracks[value.PrimaryIndex].Name ?? $"Track {value.PrimaryIndex + 1}",
            new(value.PrimaryIndex, null, DocumentId: document.Id)),
        { Kind: WorkspaceNodeKind.Segment } value => new(
            document,
            value,
            $"Segment {value.SecondaryIndex + 1}",
            new(value.PrimaryIndex, value.SecondaryIndex, DocumentId: document.Id)),
        { Kind: WorkspaceNodeKind.Route } value => new(
            document,
            value,
            document.Document.Routes[value.PrimaryIndex].Name ?? $"Route {value.PrimaryIndex + 1}",
            new(null, null, value.PrimaryIndex, DocumentId: document.Id)),
        { Kind: WorkspaceNodeKind.WaypointGroup } value => new(
            document,
            value,
            "Points of interest",
            new(null, null, DocumentId: document.Id, WaypointGroup: true)),
        { Kind: WorkspaceNodeKind.Waypoint } value => new(
            document,
            value,
            document.Document.Waypoints[value.PrimaryIndex].Name ?? $"Waypoint {value.PrimaryIndex + 1}",
            new(null, null, null, value.PrimaryIndex, document.Id)),
        _ => throw new InvalidOperationException()
    };

    public static string DocumentName(WorkspaceDocument document) =>
        document.Document.Metadata?.Name ?? document.SourceFileName ?? "Unnamed document";
}

internal sealed class DocumentTreeExpansionState
{
    private readonly HashSet<Guid> initialisedDocuments = [];
    private readonly HashSet<string> expandedNodes = [];

    public void ExpandNewDocuments(RouteWorkspace workspace)
    {
        foreach (WorkspaceDocument document in workspace.Documents.Where(item => initialisedDocuments.Add(item.Id)))
        {
            expandedNodes.Add(DocumentKey(document.Id));
            for (int track = 0; track < document.Document.Tracks.Count; track++)
            {
                expandedNodes.Add(TrackKey(document.Id, track));
            }

            if (document.Document.Waypoints.Count > 0)
            {
                expandedNodes.Add(WaypointGroupKey(document.Id));
            }
        }
    }

    public bool IsExpanded(string key) => expandedNodes.Contains(key);

    public void Toggle(string key)
    {
        if (!expandedNodes.Remove(key))
        {
            expandedNodes.Add(key);
        }
    }

    public static string DocumentKey(Guid documentId) => $"document:{documentId}";
    public static string TrackKey(Guid documentId, int track) => $"track:{documentId}:{track}";
    public static string WaypointGroupKey(Guid documentId) => $"poi:{documentId}";
}
