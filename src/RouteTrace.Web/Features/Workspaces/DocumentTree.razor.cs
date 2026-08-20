using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using RouteTrace.Core.Routes.Documents;
using RouteTrace.Core.Routes.Workspaces;

namespace RouteTrace.Web.Features.Workspaces;

public partial class DocumentTree
{
    [Parameter, EditorRequired]
    public required RouteWorkspace Workspace { get; set; }

    [Parameter, EditorRequired]
    public required IReadOnlySet<DocumentTreeNodeIdentity> SelectedNodes { get; set; }

    [Parameter]
    public EventCallback<DocumentTreeSelectionRequest> SelectionRequested { get; set; }

    [Parameter]
    public EventCallback<DocumentTreeActionRequest> ActionsRequested { get; set; }

    private readonly DocumentTreeExpansionState expansion = new();

    protected override void OnParametersSet() => expansion.ExpandNewDocuments(Workspace);

    public void ExpandTrack(Guid documentId, int trackIndex)
    {
        expansion.Expand(DocumentTreeExpansionState.TrackKey(documentId, trackIndex));
        StateHasChanged();
    }

    private Task SelectTargetAsync(DocumentTreeTarget target, MouseEventArgs args) =>
        SelectionRequested.InvokeAsync(new(target, args.CtrlKey));

    private Task OpenActionsAsync(DocumentTreeTarget target, MouseEventArgs args) =>
        ActionsRequested.InvokeAsync(new(target, args.ClientX, args.ClientY));

    private Task HandleContextKeyAsync(DocumentTreeTarget target, KeyboardEventArgs args) =>
        IsContextMenuKey(args)
            ? ActionsRequested.InvokeAsync(new(target, 24, 80))
            : Task.CompletedTask;

    private static bool IsContextMenuKey(KeyboardEventArgs args) =>
        args.Key == "ContextMenu" || (args.Key == "F10" && args.ShiftKey);

    private bool SelectsWholeDocument(Guid id) =>
        SelectedNodes.Contains(new(id, null));

    private bool IsTrackSelected(Guid id, int track) =>
        SelectsWholeDocument(id) ||
        SelectedNodes.Contains(new(id, new(WorkspaceNodeKind.Track, track)));

    private bool IsSegmentSelected(Guid id, int track, int segment) =>
        SelectsWholeDocument(id) ||
        IsTrackSelected(id, track) ||
        SelectedNodes.Contains(new(id, new(WorkspaceNodeKind.Segment, track, segment)));

    private bool IsRouteSelected(Guid id, int route) =>
        SelectsWholeDocument(id) ||
        SelectedNodes.Contains(new(id, new(WorkspaceNodeKind.Route, route)));

    private bool IsWaypointGroupSelected(Guid id) =>
        SelectsWholeDocument(id) ||
        SelectedNodes.Contains(new(id, new(WorkspaceNodeKind.WaypointGroup)));

    private bool IsWaypointSelected(Guid id, int waypoint) =>
        IsWaypointGroupSelected(id) ||
        SelectedNodes.Contains(new(id, new(WorkspaceNodeKind.Waypoint, waypoint)));

    private static string RowClass(bool active) => active
        ? "document-explorer__row document-explorer__row--colour document-explorer__row--active"
        : "document-explorer__row document-explorer__row--colour";

    private static DocumentTreeTarget TargetFrom(WorkspaceDocument document, WorkspaceNode? node) =>
        DocumentTreeTargetFactory.Create(document, node);

    private static string DocumentName(WorkspaceDocument document) =>
        DocumentTreeTargetFactory.DocumentName(document);
}
