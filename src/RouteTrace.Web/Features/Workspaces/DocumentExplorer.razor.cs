using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using RouteTrace.Core.Gpx;
using RouteTrace.Core.Routes;
using RouteTrace.Web.Features.Import;
using RouteTrace.Web.Features.Map;

namespace RouteTrace.Web.Features.Workspaces;

public partial class DocumentExplorer
{
    [Inject] private IJSRuntime JavaScript { get; set; } = null!;

    [Parameter, EditorRequired] public required RouteWorkspace Workspace { get; set; }
    [Parameter] public MapSelection Selection { get; set; }
    [Parameter] public bool Visible { get; set; }
    [Parameter] public EventCallback<RouteWorkspace> WorkspaceChanged { get; set; }
    [Parameter] public EventCallback<MapSelection> SelectionChanged { get; set; }
    [Parameter] public EventCallback<MapSelection> FocusRequested { get; set; }

    private readonly HashSet<Guid> initialisedDocuments = [];
    private readonly HashSet<string> expandedNodes = [];
    private readonly HashSet<NodeIdentity> selectedNodes = [];
    private ActionTarget? actionTarget;
    private ActionTarget? infoTarget;
    private ActionTarget? appearanceTarget;
    private double menuX;
    private double menuY;
    private string? infoName;
    private string? infoDescription;
    private string appearanceColour = "#2563eb";
    private IJSObjectReference? downloadModule;

    private async Task OpenActionsAsync(ActionTarget target, MouseEventArgs args)
    {
        var identity = new NodeIdentity(target.Document.Id, target.Node);
        if (!selectedNodes.Contains(identity))
        {
            await SelectNodeAsync(target.Document.Id, target.Node, target.Selection, false);
        }

        actionTarget = target;
        menuX = args.ClientX;
        menuY = args.ClientY;
    }

    private async Task HandleContextKeyAsync(ActionTarget target, KeyboardEventArgs args)
    {
        if (args.Key is not ("ContextMenu" or "F10") || (args.Key == "F10" && !args.ShiftKey))
        {
            return;
        }

        await OpenActionsAsync(target, new MouseEventArgs { ClientX = 24, ClientY = 80 });
    }
    private void CloseActions() => actionTarget = null;
    private IReadOnlyList<ActionTarget> SelectedTargets => selectedNodes.Select(ResolveTarget).OfType<ActionTarget>().ToArray();
    private bool AllTargetsVisible => SelectedTargets.All(TargetIsVisible);
    private bool CanEditInfo => actionTarget is { } target && SelectedTargets.Count == 1 && HasInfo(target.Node);
    private async Task ActivateTargetAsync()
    {
        if (actionTarget is not { } target) return;
        await WorkspaceChanged.InvokeAsync(Workspace.Activate(target.Document.Id));
        CloseActions();
    }
    private async Task FocusTargetAsync()
    {
        if (actionTarget is not { } target) return;
        await FocusRequested.InvokeAsync(target.Selection);
        CloseActions();
    }
    private async Task ToggleTargetVisibilityAsync()
    {
        bool visible = !AllTargetsVisible;
        RouteWorkspace changed = Workspace;
        foreach (ActionTarget target in SelectedTargets)
            changed = target.Node is null ? changed.SetVisibility(target.Document.Id, visible) : changed.SetNodeVisibility(target.Document.Id, target.Node.Value, visible);
        await WorkspaceChanged.InvokeAsync(changed); CloseActions();
    }
    private void OpenAppearance()
    {
        if (actionTarget is not { } target) return;
        appearanceTarget = target;
        appearanceColour = target.Node is null ? target.Document.Colour : target.Document.NodeColour(target.Node.Value);
        CloseActions();
    }
    private void CloseAppearance() => appearanceTarget = null;
    private bool CanResetAppearance => SelectedTargets.Count > 0 && SelectedTargets.All(target => target.Node is not null);
    private async Task SaveAppearanceAsync()
    {
        RouteWorkspace changed = Workspace;
        foreach (ActionTarget target in SelectedTargets)
            changed = target.Node is null ? changed.SetColour(target.Document.Id, appearanceColour) : changed.SetNodeColour(target.Document.Id, target.Node.Value, appearanceColour);
        await WorkspaceChanged.InvokeAsync(changed); CloseAppearance();
    }
    private async Task ResetAppearanceAsync()
    {
        RouteWorkspace changed = Workspace;
        foreach (ActionTarget target in SelectedTargets)
            if (target.Node is { } node) changed = changed.SetNodeColour(target.Document.Id, node, null);
        await WorkspaceChanged.InvokeAsync(changed); CloseAppearance();
    }
    private async Task CloseTargetAsync()
    {
        if (actionTarget is not { } target) return;
        await WorkspaceChanged.InvokeAsync(Workspace.Close(target.Document.Id));
        CloseActions();
    }
    private async Task ExportTargetAsync()
    {
        if (actionTarget is not { } action) return;
        WorkspaceDocument target = action.Document; await using var stream = new MemoryStream();
        await GpxExporter.ExportAsync(target.Document, stream, "Route Trace"); stream.Position = 0;
        using var reference = new DotNetStreamReference(stream); downloadModule ??= await JavaScript.InvokeAsync<IJSObjectReference>("import", "./generated/download.js");
        await downloadModule.InvokeVoidAsync("downloadStream", GpxDownloadFileName.From(target.Document.Metadata?.Name, target.SourceFileName), "application/gpx+xml", reference); CloseActions();
    }
    private static bool TargetIsVisible(ActionTarget target) => target.Node is null ? target.Document.IsVisible : target.Document.IsNodeVisible(target.Node.Value);
    private static bool HasInfo(WorkspaceNode? node) => node is null || node.Value.Kind is WorkspaceNodeKind.Track or WorkspaceNodeKind.Route or WorkspaceNodeKind.Waypoint;
    private bool InfoHasDescription => infoTarget?.Node is null || infoTarget?.Node?.Kind == WorkspaceNodeKind.Waypoint;
    private void OpenInfo()
    {
        if (actionTarget is not { } target) return;
        infoTarget = target; CloseActions();
        (infoName, infoDescription) = InfoFields(target);
    }

    private static (string? Name, string? Description) InfoFields(ActionTarget target) => target.Node switch
    {
        null => (target.Document.Document.Metadata?.Name, target.Document.Document.Metadata?.Description),
        { Kind: WorkspaceNodeKind.Track } node => (target.Document.Document.Tracks[node.PrimaryIndex].Name, null),
        { Kind: WorkspaceNodeKind.Route } node => (target.Document.Document.Routes[node.PrimaryIndex].Name, null),
        { Kind: WorkspaceNodeKind.Waypoint } node => WaypointInfo(target.Document.Document.Waypoints[node.PrimaryIndex]),
        _ => (null, null)
    };

    private static (string? Name, string? Description) WaypointInfo(Waypoint waypoint) =>
        (waypoint.Name, waypoint.Description);
    private void CloseInfo() { infoTarget = null; infoName = null; infoDescription = null; }
    private async Task SaveInfoAsync()
    {
        if (infoTarget is not { } target) return;
        await WorkspaceChanged.InvokeAsync(Workspace.UpdateNodeInfo(target.Document.Id, target.Node, EmptyToNull(infoName), EmptyToNull(infoDescription)));
        CloseInfo();
    }
    private static string? EmptyToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private ActionTarget? ResolveTarget(NodeIdentity identity)
    {
        WorkspaceDocument? document = Workspace.Documents.FirstOrDefault(item => item.Id == identity.DocumentId);
        return document is null ? null : TargetFrom(document, identity.Node);
    }
    private static ActionTarget TargetFrom(WorkspaceDocument document, WorkspaceNode? node) => node switch
    {
        null => new(document, null, DocumentName(document), new(null, null, DocumentId: document.Id, WholeDocument: true)),
        { Kind: WorkspaceNodeKind.Track } value => new(document, value, document.Document.Tracks[value.PrimaryIndex].Name ?? $"Track {value.PrimaryIndex + 1}", new(value.PrimaryIndex, null, DocumentId: document.Id)),
        { Kind: WorkspaceNodeKind.Segment } value => new(document, value, $"Segment {value.SecondaryIndex + 1}", new(value.PrimaryIndex, value.SecondaryIndex, DocumentId: document.Id)),
        { Kind: WorkspaceNodeKind.Route } value => new(document, value, document.Document.Routes[value.PrimaryIndex].Name ?? $"Route {value.PrimaryIndex + 1}", new(null, null, value.PrimaryIndex, DocumentId: document.Id)),
        { Kind: WorkspaceNodeKind.WaypointGroup } value => new(document, value, "Points of interest", new(null, null, DocumentId: document.Id, WaypointGroup: true)),
        { Kind: WorkspaceNodeKind.Waypoint } value => new(document, value, document.Document.Waypoints[value.PrimaryIndex].Name ?? $"Waypoint {value.PrimaryIndex + 1}", new(null, null, null, value.PrimaryIndex, document.Id)),
        _ => throw new InvalidOperationException()
    };
    private sealed record ActionTarget(WorkspaceDocument Document, WorkspaceNode? Node, string Name, MapSelection Selection);
    private readonly record struct NodeIdentity(Guid DocumentId, WorkspaceNode? Node);

    protected override void OnParametersSet()
    {
        foreach (WorkspaceDocument workspaceDocument in Workspace.Documents.Where(document => initialisedDocuments.Add(document.Id)))
        {
            expandedNodes.Add($"document:{workspaceDocument.Id}");
            for (int track = 0; track < workspaceDocument.Document.Tracks.Count; track++) expandedNodes.Add($"track:{workspaceDocument.Id}:{track}");
            if (workspaceDocument.Document.Waypoints.Count > 0) expandedNodes.Add($"poi:{workspaceDocument.Id}");
        }
    }

    private void Toggle(string key)
    {
        if (!expandedNodes.Remove(key)) expandedNodes.Add(key);
    }

    private bool SelectsWholeDocument(Guid id) => selectedNodes.Contains(new(id, null));
    private static string RowClass(bool active) => active
        ? "document-explorer__row document-explorer__row--colour document-explorer__row--active"
        : "document-explorer__row document-explorer__row--colour";

    private bool IsTrackSelected(Guid id, int track) => SelectsWholeDocument(id) || selectedNodes.Contains(new(id, new(WorkspaceNodeKind.Track, track)));
    private bool IsSegmentSelected(Guid id, int track, int segment) => SelectsWholeDocument(id) || IsTrackSelected(id, track) || selectedNodes.Contains(new(id, new(WorkspaceNodeKind.Segment, track, segment)));
    private bool IsRouteSelected(Guid id, int route) => SelectsWholeDocument(id) || selectedNodes.Contains(new(id, new(WorkspaceNodeKind.Route, route)));
    private bool IsWaypointGroupSelected(Guid id) => SelectsWholeDocument(id) || selectedNodes.Contains(new(id, new(WorkspaceNodeKind.WaypointGroup)));
    private bool IsWaypointSelected(Guid id, int waypoint) => IsWaypointGroupSelected(id) || selectedNodes.Contains(new(id, new(WorkspaceNodeKind.Waypoint, waypoint)));

    private async Task SelectNodeAsync(Guid id, WorkspaceNode? node, MapSelection selection, bool additive)
    {
        var identity = new NodeIdentity(id, node);
        if (!additive) { selectedNodes.Clear(); selectedNodes.Add(identity); }
        else if (!selectedNodes.Remove(identity)) selectedNodes.Add(identity);
        await WorkspaceChanged.InvokeAsync(Workspace.Activate(id).Select(id));
        MapSelection mapSelection = selectedNodes.Contains(identity)
            ? selection with { DocumentId = id }
            : selectedNodes.Count == 0 ? MapSelection.None : ResolveTarget(selectedNodes.First())?.Selection ?? MapSelection.None;
        await SelectionChanged.InvokeAsync(mapSelection);
    }

    private Task SelectTargetAsync(ActionTarget target, MouseEventArgs args) =>
        SelectNodeAsync(target.Document.Id, target.Node, target.Selection, args.CtrlKey);

    private static string DocumentName(WorkspaceDocument document) => document.Document.Metadata?.Name ?? document.SourceFileName ?? "Unnamed document";
}
