using Microsoft.AspNetCore.Components;
using RouteTrace.Core.Routes;
using RouteTrace.Web.Features.Map;

namespace RouteTrace.Web.Features.Workspaces;

public partial class DocumentExplorer
{
    [Parameter, EditorRequired]
    public required RouteWorkspace Workspace { get; set; }

    [Parameter]
    public MapSelection Selection { get; set; }

    [Parameter]
    public bool Visible { get; set; }

    [Parameter]
    public EventCallback<RouteWorkspace> WorkspaceChanged { get; set; }

    [Parameter]
    public EventCallback<MapSelection> SelectionChanged { get; set; }

    [Parameter]
    public EventCallback<MapSelection> FocusRequested { get; set; }

    private readonly HashSet<DocumentTreeNodeIdentity> selectedNodes = [];
    private DocumentExplorerActions? actions;

    private async Task OpenActionsAsync(DocumentTreeActionRequest request)
    {
        DocumentTreeTarget target = request.Target;
        var identity = new DocumentTreeNodeIdentity(target.Document.Id, target.Node);
        if (!selectedNodes.Contains(identity))
        {
            await SelectNodeAsync(target, false);
        }

        actions?.Open(request, SelectedTargets);
    }

    private Task SelectTargetAsync(DocumentTreeSelectionRequest request) =>
        SelectNodeAsync(request.Target, request.Additive);

    private async Task SelectNodeAsync(DocumentTreeTarget target, bool additive)
    {
        var identity = new DocumentTreeNodeIdentity(target.Document.Id, target.Node);
        if (!additive)
        {
            selectedNodes.Clear();
            selectedNodes.Add(identity);
        }
        else if (!selectedNodes.Remove(identity))
        {
            selectedNodes.Add(identity);
        }

        RouteWorkspace changed = Workspace
            .Activate(target.Document.Id)
            .Select(target.Document.Id);
        await WorkspaceChanged.InvokeAsync(changed);

        MapSelection mapSelection = selectedNodes.Contains(identity)
            ? target.Selection
            : RemainingSelection();
        await SelectionChanged.InvokeAsync(mapSelection);
    }

    private MapSelection RemainingSelection() => selectedNodes.Count == 0
        ? MapSelection.None
        : ResolveTarget(selectedNodes.First())?.Selection ?? MapSelection.None;

    private IReadOnlyList<DocumentTreeTarget> SelectedTargets =>
        selectedNodes
            .Select(ResolveTarget)
            .OfType<DocumentTreeTarget>()
            .ToArray();

    private DocumentTreeTarget? ResolveTarget(DocumentTreeNodeIdentity identity)
    {
        WorkspaceDocument? document = Workspace.Documents.FirstOrDefault(
            item => item.Id == identity.DocumentId);
        return document is null
            ? null
            : DocumentTreeTargetFactory.Create(document, identity.Node);
    }
}
