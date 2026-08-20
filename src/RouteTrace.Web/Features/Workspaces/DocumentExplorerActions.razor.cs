using Microsoft.AspNetCore.Components;
using RouteTrace.Core.Routes.Documents;
using RouteTrace.Core.Routes.Workspaces;
using RouteTrace.Web.Features.Import;
using RouteTrace.Web.Features.Map;

namespace RouteTrace.Web.Features.Workspaces;

public partial class DocumentExplorerActions
{
    [Inject] private GpxExportOperation GpxExport { get; set; } = null!;

    [Parameter, EditorRequired]
    public required RouteWorkspace Workspace { get; set; }

    [Parameter]
    public EventCallback<RouteWorkspace> WorkspaceChanged { get; set; }

    [Parameter]
    public EventCallback<MapSelection> FocusRequested { get; set; }

    [Parameter]
    public EventCallback<DocumentTreeTarget> EditRequested { get; set; }

    [Parameter]
    public EventCallback<DocumentTreeNodeIdentity> TrackExpansionRequested { get; set; }

    private IReadOnlyList<DocumentTreeTarget> selectedTargets = [];
    private DocumentTreeTarget? actionTarget;
    private DocumentTreeTarget? infoTarget;
    private DocumentTreeTarget? appearanceTarget;
    private double menuX;
    private double menuY;
    private string? infoName;
    private string? infoDescription;
    private string appearanceColour = "#2563eb";

    public void Open(DocumentTreeActionRequest request, IReadOnlyList<DocumentTreeTarget> targets)
    {
        actionTarget = request.Target;
        selectedTargets = targets;
        menuX = request.ClientX;
        menuY = request.ClientY;
    }

    public void OpenBackground(double clientX, double clientY)
    {
        actionTarget = null;
        selectedTargets = [];
        menuX = clientX;
        menuY = clientY;
        backgroundActionsOpen = true;
    }

    private bool backgroundActionsOpen;
    private bool ActionsOpen => backgroundActionsOpen || actionTarget is not null;
    private string ActionLabel => actionTarget?.Name ?? "document explorer";
    private string MenuPositionStyle => actionTarget is null
        ? FormattableString.Invariant($"left:min({menuX}px, calc(100vw - 11rem)); top:min({menuY}px, calc(100vh - 3rem))")
        : FormattableString.Invariant($"left:min({menuX}px, calc(100vw - 11rem)); top:min({menuY}px, calc(100vh - 22rem))");

    private bool AllTargetsVisible => selectedTargets.All(TargetIsVisible);
    private bool CanEditInfo => actionTarget is { } target && selectedTargets.Count == 1 && HasInfo(target.Node);
    private bool CanResetAppearance => selectedTargets.Count > 0 && selectedTargets.All(target => target.Node is not null);
    private bool CanDeleteTarget => actionTarget?.Node is null ||
        actionTarget.Node.Value.Kind is WorkspaceNodeKind.Track or WorkspaceNodeKind.Segment or WorkspaceNodeKind.Route;
    private bool InfoHasDescription => infoTarget?.Node is null || infoTarget?.Node?.Kind == WorkspaceNodeKind.Waypoint;

    private async Task ActivateTargetAsync()
    {
        if (actionTarget is not { } target)
        {
            return;
        }

        await WorkspaceChanged.InvokeAsync(Workspace.Activate(target.Document.Id));
        CloseActions();
    }

    private async Task FocusTargetAsync()
    {
        if (actionTarget is not { } target)
        {
            return;
        }

        await FocusRequested.InvokeAsync(target.Selection);
        CloseActions();
    }

    private async Task ToggleTargetVisibilityAsync()
    {
        bool visible = !AllTargetsVisible;
        RouteWorkspace changed = Workspace;
        foreach (DocumentTreeTarget target in selectedTargets)
        {
            changed = target.Node is null
                ? changed.SetVisibility(target.Document.Id, visible)
                : changed.SetNodeVisibility(target.Document.Id, target.Node.Value, visible);
        }

        await WorkspaceChanged.InvokeAsync(changed);
        CloseActions();
    }

    private void OpenAppearance()
    {
        if (actionTarget is not { } target)
        {
            return;
        }

        appearanceTarget = target;
        appearanceColour = target.Node is null
            ? target.Document.Colour
            : target.Document.NodeColour(target.Node.Value);
        CloseActions();
    }

    private async Task SaveAppearanceAsync()
    {
        RouteWorkspace changed = Workspace;
        foreach (DocumentTreeTarget target in selectedTargets)
        {
            changed = target.Node is null
                ? changed.SetColour(target.Document.Id, appearanceColour)
                : changed.SetNodeColour(target.Document.Id, target.Node.Value, appearanceColour);
        }

        await WorkspaceChanged.InvokeAsync(changed);
        CloseAppearance();
    }

    private async Task ResetAppearanceAsync()
    {
        RouteWorkspace changed = Workspace;
        foreach (DocumentTreeTarget target in selectedTargets)
        {
            if (target.Node is { } node)
            {
                changed = changed.SetNodeColour(target.Document.Id, node, null);
            }
        }

        await WorkspaceChanged.InvokeAsync(changed);
        CloseAppearance();
    }

    private async Task CloseTargetAsync()
    {
        if (actionTarget is not { } target)
        {
            return;
        }

        await WorkspaceChanged.InvokeAsync(Workspace.Close(target.Document.Id));
        CloseActions();
    }

    private async Task CreateDocumentAsync()
    {
        var document = new RouteDocument(metadata: new RouteMetadata("Untitled document"));
        await WorkspaceChanged.InvokeAsync(Workspace.AddDocument(document));
        CloseActions();
    }

    private async Task CreateTrackAsync()
    {
        if (actionTarget is not { Node: null } target) return;
        await WorkspaceChanged.InvokeAsync(Workspace.AddTrack(target.Document.Id));
        CloseActions();
    }

    private async Task CreateSegmentAsync()
    {
        if (actionTarget?.Node is not { Kind: WorkspaceNodeKind.Track } node) return;
        await WorkspaceChanged.InvokeAsync(Workspace.AddSegment(actionTarget.Document.Id, node.PrimaryIndex));
        await TrackExpansionRequested.InvokeAsync(new(actionTarget.Document.Id, node));
        CloseActions();
    }

    private async Task EditTargetAsync()
    {
        if (actionTarget is not { } target) return;
        CloseActions();
        await EditRequested.InvokeAsync(target);
    }

    private async Task DeleteTargetAsync()
    {
        if (actionTarget is not { Node: { } node } target) return;
        await WorkspaceChanged.InvokeAsync(Workspace.DeleteNode(target.Document.Id, node));
        CloseActions();
    }

    private Task DeleteOrCloseTargetAsync() => actionTarget?.Node is null
        ? CloseTargetAsync()
        : DeleteTargetAsync();

    private async Task ExportTargetAsync()
    {
        if (actionTarget is not { } action)
        {
            return;
        }

        await GpxExport.ExecuteAsync(action.Document);
        CloseActions();
    }

    private void OpenInfo()
    {
        if (actionTarget is not { } target)
        {
            return;
        }

        infoTarget = target;
        CloseActions();
        (infoName, infoDescription) = InfoFields(target);
    }

    private async Task SaveInfoAsync()
    {
        if (infoTarget is not { } target)
        {
            return;
        }

        RouteWorkspace changed = Workspace.UpdateNodeInfo(
            target.Document.Id,
            target.Node,
            EmptyToNull(infoName),
            EmptyToNull(infoDescription));
        await WorkspaceChanged.InvokeAsync(changed);
        CloseInfo();
    }

    private void CloseActions()
    {
        actionTarget = null;
        backgroundActionsOpen = false;
    }
    private void CloseAppearance() => appearanceTarget = null;

    private void CloseInfo()
    {
        infoTarget = null;
        infoName = null;
        infoDescription = null;
    }

    private static bool TargetIsVisible(DocumentTreeTarget target) => target.Node is null
        ? target.Document.IsVisible
        : target.Document.IsNodeVisible(target.Node.Value);

    private static bool HasInfo(WorkspaceNode? node) =>
        node is null ||
        node.Value.Kind is WorkspaceNodeKind.Track or WorkspaceNodeKind.Route or WorkspaceNodeKind.Waypoint;

    private static (string? Name, string? Description) InfoFields(DocumentTreeTarget target) => target.Node switch
    {
        null => (target.Document.Document.Metadata?.Name, target.Document.Document.Metadata?.Description),
        { Kind: WorkspaceNodeKind.Track } node =>
            (target.Document.Document.Tracks[node.PrimaryIndex].Name, null),
        { Kind: WorkspaceNodeKind.Route } node =>
            (target.Document.Document.Routes[node.PrimaryIndex].Name, null),
        { Kind: WorkspaceNodeKind.Waypoint } node =>
            WaypointInfo(target.Document.Document.Waypoints[node.PrimaryIndex]),
        _ => (null, null)
    };

    private static (string? Name, string? Description) WaypointInfo(Waypoint waypoint) =>
        (waypoint.Name, waypoint.Description);

    private static string? EmptyToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

}
