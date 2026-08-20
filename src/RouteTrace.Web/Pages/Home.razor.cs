using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using RouteTrace.Core.Editing;
using RouteTrace.Core.Routes.Documents;
using RouteTrace.Core.Routes.Geometry;
using RouteTrace.Core.Routes.Workspaces;
using RouteTrace.Web.Features.Import;
using RouteTrace.Web.Features.Map;
using RouteTrace.Web.Features.Workspaces;

namespace RouteTrace.Web.Pages;

public partial class Home
{
    [Inject] private IWorkspaceStore WorkspaceStore { get; set; } = null!;
    [Inject] private IJSRuntime JavaScript { get; set; } = null!;

    private RouteWorkspace workspace = new(Guid.NewGuid(), "Untitled workspace");
    private MapSelection selection = MapSelection.None;
    private bool inspectorVisible = true;
    private bool explorerVisible = true;
    private int focusVersion;
    private IReadOnlyList<MapDocumentGeometry> MapDocuments { get; set; } = [];
    private readonly Dictionary<Guid, (RouteDocument Document, MapGeometry Geometry)> geometryCache = [];
    private ManualRouteEditor? manualEditor;
    private Guid? manualDocumentId;
    private EditableLineTarget? manualTarget;
    private RouteDocument? manualOriginalDocument;
    private string manualEditorTitle = "Manual route";
    private int? selectedEditingPoint;
    private bool manualEditDirty;
    private bool editExitConfirmationVisible;
    private IReadOnlyList<double[]> ManualEditingPoints => manualEditor?.Points
        .Select(point => new[] { point.Longitude, point.Latitude })
        .ToArray() ?? [];

    protected override async Task OnInitializedAsync()
    {
        WorkspaceDecodeResult? restored = await WorkspaceStore.OpenMostRecentAsync();
        if (restored?.Workspace is { } restoredWorkspace)
        {
            workspace = restoredWorkspace;
        }
        RefreshMapDocuments();
    }

    private async Task HandleDocumentImported(ImportedGpxDocument importedDocument)
    {
        workspace = workspace.AddDocument(importedDocument.Document, importedDocument.SourceFileName);
        RefreshMapDocuments();
        selection = MapSelection.None;
        await InvokeAsync(StateHasChanged);
        await JavaScript.InvokeVoidAsync("routeTrace.waitForAnimationFrame");
        await WorkspaceStore.SaveAsync(workspace);
    }

    private async Task HandleWorkspaceChangedAsync(RouteWorkspace updatedWorkspace)
    {
        await WorkspaceStore.SaveAsync(updatedWorkspace);
        workspace = updatedWorkspace;
        RefreshMapDocuments();
        selection = MapSelection.None;
        if (manualDocumentId is { } documentId &&
            (updatedWorkspace.Documents.FirstOrDefault(document => document.Id == documentId) is not { } editedDocument ||
             manualTarget is not { } target || !TargetExists(editedDocument.Document, target)))
        {
            FinishManualRoute();
        }
    }

    private void HandleWorkspaceDeleted(Guid workspaceId)
    {
        if (workspace.Id == workspaceId)
        {
            workspace = new RouteWorkspace(Guid.NewGuid(), "Untitled workspace");
            RefreshMapDocuments();
            FinishManualRoute();
        }
    }

    private async Task StartEditingTargetAsync(DocumentTreeTarget requestedTarget)
    {
        if (requestedTarget.Node is not { } node) return;
        EditableLineTarget? target = node.Kind switch
        {
            WorkspaceNodeKind.Segment => EditableLineTarget.TrackSegment(node.PrimaryIndex, node.SecondaryIndex),
            WorkspaceNodeKind.Route => EditableLineTarget.Route(node.PrimaryIndex),
            _ => null
        };
        if (target is null) return;
        EditableLineTarget editableTarget = target.Value;
        Guid documentId = requestedTarget.Document.Id;
        WorkspaceDocument document = workspace.Documents.Single(item => item.Id == documentId);
        manualEditor = new ManualRouteEditor(editableTarget.GetPoints(document.Document));
        manualDocumentId = documentId;
        manualTarget = editableTarget;
        manualOriginalDocument = document.Document;
        manualEditorTitle = editableTarget.Kind == EditableLineKind.Route
            ? document.Document.Routes[editableTarget.PrimaryIndex].Name ?? $"Route {editableTarget.PrimaryIndex + 1}"
            : $"Segment {editableTarget.SecondaryIndex + 1}";
        selectedEditingPoint = null;
        manualEditDirty = false;
        editExitConfirmationVisible = false;
        workspace = workspace.Activate(documentId).Select(documentId);
        RefreshMapDocuments();
        focusVersion++;
        await WorkspaceStore.SaveAsync(workspace);
    }

    private void FinishManualRoute()
    {
        manualEditor = null;
        manualDocumentId = null;
        manualTarget = null;
        manualOriginalDocument = null;
        manualEditorTitle = "Manual route";
        selectedEditingPoint = null;
        manualEditDirty = false;
        editExitConfirmationVisible = false;
    }

    private async Task HandleEditingPointAddedAsync(double[] coordinate)
    {
        if (manualEditor is null || coordinate.Length < 2) return;
        var point = new GeoCoordinate(coordinate[1], coordinate[0]);
        manualEditor.Add(point);
        selectedEditingPoint = manualEditor.Points.Count - 1;
        await CommitManualEditAsync();
    }

    private void HandleEditingPointSelected(int index)
    {
        selectedEditingPoint = index >= 0 ? index : null;
    }

    private async Task HandleEditingPointMovedAsync(EditingPointMove move)
    {
        if (manualEditor is null || move.Index < 0 || move.Index >= EditablePointCount()) return;
        manualEditor.Move(move.Index, new GeoCoordinate(move.Coordinate[1], move.Coordinate[0]));
        selectedEditingPoint = move.Index;
        await CommitManualEditAsync();
    }

    private async Task HandleEditingPointsReplacedAsync(double[][] coordinates)
    {
        if (manualEditor is null || coordinates.Any(coordinate => coordinate.Length < 2)) return;
        GeoCoordinate[] replacement = coordinates
            .Select(coordinate => new GeoCoordinate(coordinate[1], coordinate[0]))
            .ToArray();
        IReadOnlyList<GeoCoordinate> previous = manualEditor.Points;
        int changedIndex = FirstChangedIndex(previous, replacement);
        if (!manualEditor.ReplaceCoordinates(replacement)) return;
        selectedEditingPoint = Math.Min(changedIndex, EditablePointCount() - 1);
        await CommitManualEditAsync();
    }

    private async Task DeleteEditingPointAsync()
    {
        if (manualEditor is null || selectedEditingPoint is not { } selected) return;
        manualEditor.Delete(selected);
        selectedEditingPoint = manualEditor.Points.Count == 0
            ? null
            : Math.Min(selected, EditablePointCount() - 1);
        await CommitManualEditAsync();
    }

    private async Task DeleteEditingPointAtAsync(int index)
    {
        if (index < 0 || index >= EditablePointCount()) return;
        selectedEditingPoint = index;
        await DeleteEditingPointAsync();
    }

    private async Task ReverseManualRouteAsync()
    {
        if (manualEditor is null) return;
        GeoCoordinate? selected = selectedEditingPoint is { } index ? manualEditor.Points[index] : null;
        manualEditor.Reverse();
        selectedEditingPoint = selected is null
            ? null
            : manualEditor.Points.Take(EditablePointCount()).ToList().IndexOf(selected.Value);
        await CommitManualEditAsync();
    }

    private async Task CloseManualRouteLoopAsync()
    {
        if (manualEditor is null) return;
        manualEditor.CloseLoop();
        await CommitManualEditAsync();
    }

    private async Task ClearManualRouteAsync()
    {
        if (manualEditor is null) return;
        manualEditor.Clear();
        selectedEditingPoint = null;
        await CommitManualEditAsync();
    }

    private async Task UndoManualEditAsync()
    {
        if (manualEditor?.Undo() != true) return;
        selectedEditingPoint = null;
        await CommitManualEditAsync();
    }

    private async Task RedoManualEditAsync()
    {
        if (manualEditor?.Redo() != true) return;
        selectedEditingPoint = null;
        await CommitManualEditAsync();
    }

    private int EditablePointCount() => manualEditor is null
        ? 0
        : manualEditor.Points.Count - (manualEditor.IsLoop ? 1 : 0);

    private async Task CommitManualEditAsync()
    {
        if (manualEditor is null || manualDocumentId is not { } documentId || manualTarget is not { } target) return;
        WorkspaceDocument current = workspace.Documents.Single(document => document.Id == documentId);
        RouteDocument changed = target.ReplacePoints(current.Document, manualEditor.RoutePoints);
        workspace = workspace.ReplaceDocument(documentId, changed);
        manualEditDirty = manualOriginalDocument is not null &&
            !target.GetPoints(manualOriginalDocument).SequenceEqual(manualEditor.RoutePoints);
        RefreshMapDocuments();
        await WorkspaceStore.SaveAsync(workspace);
    }

    private Task RequestCloseManualRouteAsync()
    {
        if (manualEditor is null) return Task.CompletedTask;
        if (!manualEditDirty)
        {
            FinishManualRoute();
            return Task.CompletedTask;
        }

        editExitConfirmationVisible = true;
        return Task.CompletedTask;
    }

    private void KeepManualRouteChanges() => FinishManualRoute();

    private async Task DiscardManualRouteChangesAsync()
    {
        if (manualDocumentId is { } documentId && manualOriginalDocument is { } original)
        {
            workspace = workspace.ReplaceDocument(documentId, original);
            RefreshMapDocuments();
            await WorkspaceStore.SaveAsync(workspace);
        }
        FinishManualRoute();
    }

    private void CancelCloseManualRoute() => editExitConfirmationVisible = false;

    private static bool TargetExists(RouteDocument document, EditableLineTarget target) => target.Kind switch
    {
        EditableLineKind.Route => target.PrimaryIndex >= 0 && target.PrimaryIndex < document.Routes.Count,
        _ => target.PrimaryIndex >= 0 && target.PrimaryIndex < document.Tracks.Count &&
             target.SecondaryIndex >= 0 && target.SecondaryIndex < document.Tracks[target.PrimaryIndex].Segments.Count
    };

    private static int FirstChangedIndex(IReadOnlyList<GeoCoordinate> previous, IReadOnlyList<GeoCoordinate> replacement)
    {
        int common = Math.Min(previous.Count, replacement.Count);
        int index = 0;
        while (index < common && previous[index] == replacement[index]) index++;
        return Math.Min(index, Math.Max(0, replacement.Count - 1));
    }

    private void HandleSelectionChanged(MapSelection newSelection) => selection = newSelection;
    private void HandleFocusRequested(MapSelection _) => focusVersion++;

    private void HandleInspectorVisibilityChanged(bool visible) => inspectorVisible = visible;

    private void HandleExplorerVisibilityChanged(bool visible) => explorerVisible = visible;

    private void RefreshMapDocuments()
    {
        foreach (Guid removedId in geometryCache.Keys.Except(workspace.Documents.Select(document => document.Id)).ToArray())
            geometryCache.Remove(removedId);

        MapDocuments = workspace.Documents
            .Where(document => document.IsVisible)
            .Select(document => new MapDocumentGeometry(
                document.Id,
                GetGeometry(document),
                document.Colour,
                document.Id == workspace.ActiveDocumentId,
                document.Id == workspace.SelectedDocumentId,
                BuildPresentation(document)))
            .ToArray();
    }

    private MapGeometry GetGeometry(WorkspaceDocument document)
    {
        if (geometryCache.TryGetValue(document.Id, out var cached)
            && ReferenceEquals(cached.Document, document.Document))
            return cached.Geometry;

        MapGeometry geometry = MapGeometry.FromDocument(document.Document);
        geometryCache[document.Id] = (document.Document, geometry);
        return geometry;
    }

    private static IReadOnlyList<MapFeaturePresentation> BuildPresentation(WorkspaceDocument document)
    {
        var result = new List<MapFeaturePresentation>();
        for (int track = 0; track < document.Document.Tracks.Count; track++)
            for (int segment = 0; segment < document.Document.Tracks[track].Segments.Count; segment++)
            {
                var node = new WorkspaceNode(WorkspaceNodeKind.Segment, track, segment);
                result.Add(new("track", track, segment, document.IsNodeVisible(node), document.NodeColour(node)));
            }
        for (int route = 0; route < document.Document.Routes.Count; route++)
        {
            var node = new WorkspaceNode(WorkspaceNodeKind.Route, route);
            result.Add(new("route", route, -1, document.IsNodeVisible(node), document.NodeColour(node)));
        }
        for (int waypoint = 0; waypoint < document.Document.Waypoints.Count; waypoint++)
        {
            var node = new WorkspaceNode(WorkspaceNodeKind.Waypoint, waypoint);
            result.Add(new("waypoint", waypoint, -1, document.IsNodeVisible(node), document.NodeColour(node)));
        }
        return result;
    }
}
