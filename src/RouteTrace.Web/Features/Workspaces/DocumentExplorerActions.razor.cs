using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using RouteTrace.Core.Gpx;
using RouteTrace.Core.Routes.Documents;
using RouteTrace.Core.Routes.Workspaces;
using RouteTrace.Web.Features.Import;
using RouteTrace.Web.Features.Map;

namespace RouteTrace.Web.Features.Workspaces;

public partial class DocumentExplorerActions : IAsyncDisposable
{
    [Inject] private IJSRuntime JavaScript { get; set; } = null!;

    [Parameter, EditorRequired]
    public required RouteWorkspace Workspace { get; set; }

    [Parameter]
    public EventCallback<RouteWorkspace> WorkspaceChanged { get; set; }

    [Parameter]
    public EventCallback<MapSelection> FocusRequested { get; set; }

    private IReadOnlyList<DocumentTreeTarget> selectedTargets = [];
    private DocumentTreeTarget? actionTarget;
    private DocumentTreeTarget? infoTarget;
    private DocumentTreeTarget? appearanceTarget;
    private double menuX;
    private double menuY;
    private string? infoName;
    private string? infoDescription;
    private string appearanceColour = "#2563eb";
    private IJSObjectReference? downloadModule;

    public void Open(DocumentTreeActionRequest request, IReadOnlyList<DocumentTreeTarget> targets)
    {
        actionTarget = request.Target;
        selectedTargets = targets;
        menuX = request.ClientX;
        menuY = request.ClientY;
    }

    private bool AllTargetsVisible => selectedTargets.All(TargetIsVisible);
    private bool CanEditInfo => actionTarget is { } target && selectedTargets.Count == 1 && HasInfo(target.Node);
    private bool CanResetAppearance => selectedTargets.Count > 0 && selectedTargets.All(target => target.Node is not null);
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

    private async Task ExportTargetAsync()
    {
        if (actionTarget is not { } action)
        {
            return;
        }

        WorkspaceDocument target = action.Document;
        await using var stream = new MemoryStream();
        await GpxExporter.ExportAsync(target.Document, stream, "Route Trace");
        stream.Position = 0;

        using var reference = new DotNetStreamReference(stream);
        downloadModule ??= await JavaScript.InvokeAsync<IJSObjectReference>("import", "./generated/download.js");
        string fileName = GpxDownloadFileName.From(
            target.Document.Metadata?.Name,
            target.SourceFileName);
        await downloadModule.InvokeVoidAsync(
            "downloadStream",
            fileName,
            "application/gpx+xml",
            reference);
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

    private void CloseActions() => actionTarget = null;
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

    public async ValueTask DisposeAsync()
    {
        if (downloadModule is not null)
        {
            await downloadModule.DisposeAsync();
        }
    }
}
