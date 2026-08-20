using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using RouteTrace.Core.Editing;
using RouteTrace.Core.Routes.Documents;
using RouteTrace.Core.Routes.Geometry;
using RouteTrace.Core.Routes.Workspaces;
using RouteTrace.Core.Routing;
using RouteTrace.Web.Features.Import;
using RouteTrace.Web.Features.Map;
using RouteTrace.Web.Features.Workspaces;

namespace RouteTrace.Web.Pages;

public partial class Home
{
    private const string RoutingProfileStorageKey = "routeTrace.routingProfile";

    [Inject] private IWorkspaceStore WorkspaceStore { get; set; } = null!;
    [Inject] private IJSRuntime JavaScript { get; set; } = null!;
    [Inject] private IRoutePlanner RoutePlanner { get; set; } = null!;

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
    private CancellationTokenSource? routingCancellation;
    private RoutingUiState routingState = RoutingUiState.Idle;
    private string? routingMessage;
    private BicycleRoutingProfile routingProfile = BicycleRoutingProfile.Cycling;
    private IReadOnlyList<double[]> ManualEditingGeometry => manualEditor?.Points
        .Select(point => new[] { point.Longitude, point.Latitude })
        .ToArray() ?? [];
    private IReadOnlyList<double[]> ManualEditingAnchors => manualEditor?.Anchors
        .Select(point => new[] { point.Longitude, point.Latitude })
        .ToArray() ?? [];

    protected override async Task OnInitializedAsync()
    {
        string? storedProfile = await JavaScript.InvokeAsync<string?>("localStorage.getItem", RoutingProfileStorageKey);
        if (Enum.TryParse(storedProfile, true, out BicycleRoutingProfile parsedProfile) &&
            Enum.IsDefined(parsedProfile))
        {
            routingProfile = parsedProfile;
        }
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
        manualEditor = new ManualRouteEditor(
            editableTarget.GetPoints(document.Document),
            editableTarget.GetAnchorIndices(document.Document));
        manualDocumentId = documentId;
        manualTarget = editableTarget;
        manualOriginalDocument = document.Document;
        manualEditorTitle = editableTarget.Kind == EditableLineKind.Route
            ? document.Document.Routes[editableTarget.PrimaryIndex].Name ?? $"Route {editableTarget.PrimaryIndex + 1}"
            : $"Segment {editableTarget.SecondaryIndex + 1}";
        selectedEditingPoint = null;
        manualEditDirty = false;
        editExitConfirmationVisible = false;
        routingState = RoutingUiState.Idle;
        routingMessage = null;
        workspace = workspace.Activate(documentId).Select(documentId);
        RefreshMapDocuments();
        focusVersion++;
        await WorkspaceStore.SaveAsync(workspace);
    }

    private void FinishManualRoute()
    {
        routingCancellation?.Cancel();
        routingCancellation?.Dispose();
        routingCancellation = null;
        manualEditor = null;
        manualDocumentId = null;
        manualTarget = null;
        manualOriginalDocument = null;
        manualEditorTitle = "Manual route";
        selectedEditingPoint = null;
        manualEditDirty = false;
        editExitConfirmationVisible = false;
        routingState = RoutingUiState.Idle;
        routingMessage = null;
    }

    private async Task HandleEditingPointAddedAsync(double[] coordinate)
    {
        if (manualEditor is null || coordinate.Length < 2) return;
        RoutePoint[] anchors =
        [
            .. manualEditor.AnchorPoints,
            new RoutePoint(new GeoCoordinate(coordinate[1], coordinate[0]))
        ];
        await ApplyAnchorEditAsync(anchors, false, anchors.Length - 1);
    }

    private async Task HandleEditingAnchorInsertedAsync(EditingAnchorInsert insertion)
    {
        if (manualEditor is null || insertion.Coordinate.Length < 2 ||
            insertion.AfterAnchorIndex < 0 || insertion.AfterAnchorIndex >= manualEditor.AnchorPoints.Count) return;
        var anchors = manualEditor.AnchorPoints.ToList();
        anchors.Insert(
            insertion.AfterAnchorIndex + 1,
            new RoutePoint(new GeoCoordinate(insertion.Coordinate[1], insertion.Coordinate[0])));
        await ApplyAnchorEditAsync(anchors, manualEditor.IsLoop, insertion.AfterAnchorIndex + 1);
    }

    private void HandleEditingPointSelected(int index)
    {
        selectedEditingPoint = index >= 0 ? index : null;
    }

    private async Task HandleEditingPointMovedAsync(EditingPointMove move)
    {
        if (manualEditor is null || move.Coordinate.Length < 2 ||
            move.Index < 0 || move.Index >= EditablePointCount()) return;
        RoutePoint[] anchors = [.. manualEditor.AnchorPoints];
        anchors[move.Index] = anchors[move.Index] with
        {
            Coordinate = new GeoCoordinate(move.Coordinate[1], move.Coordinate[0])
        };
        await ApplyAnchorEditAsync(anchors, manualEditor.IsLoop, move.Index);
    }

    private async Task DeleteEditingPointAsync()
    {
        if (manualEditor is null || selectedEditingPoint is not { } selected) return;
        RoutePoint[] anchors = manualEditor.AnchorPoints.Where((_, index) => index != selected).ToArray();
        bool loop = manualEditor.IsLoop && anchors.Length >= 2;
        await ApplyAnchorEditAsync(
            anchors,
            loop,
            anchors.Length == 0 ? null : Math.Min(selected, anchors.Length - 1));
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
        GeoCoordinate? selected = selectedEditingPoint is { } index ? manualEditor.Anchors[index] : null;
        manualEditor.Reverse();
        selectedEditingPoint = selected is null
            ? null
            : manualEditor.Anchors.ToList().IndexOf(selected.Value);
        await CommitManualEditAsync();
    }

    private async Task CloseManualRouteLoopAsync()
    {
        if (manualEditor is null) return;
        await ApplyAnchorEditAsync(manualEditor.AnchorPoints, true, selectedEditingPoint);
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

    private int EditablePointCount() => manualEditor?.AnchorPoints.Count ?? 0;

    private async Task HandleRoutingProfileChangedAsync(BicycleRoutingProfile profile)
    {
        if (profile == routingProfile) return;
        routingProfile = profile;
        await JavaScript.InvokeVoidAsync("localStorage.setItem", RoutingProfileStorageKey, profile.ToString());
        if (manualEditor is not { } editor || editor.AnchorPoints.Count < 2)
        {
            await InvokeAsync(StateHasChanged);
            return;
        }

        await ApplyAnchorEditAsync(editor.AnchorPoints, editor.IsLoop, selectedEditingPoint, true);
    }

    private async Task ApplyAnchorEditAsync(
        IReadOnlyList<RoutePoint> anchors,
        bool isLoop,
        int? selectedIndex,
        bool rerouteAll = false)
    {
        if (manualEditor is not { } editor) return;
        (IReadOnlyList<IReadOnlyList<RoutePoint>> legs, IReadOnlyList<int> affected) =
            BuildProposedLegs(editor.AnchorPoints, editor.Legs, editor.IsLoop, anchors, isLoop, rerouteAll);
        if (affected.Count == 0)
        {
            editor.ApplyRoutedEdit(anchors, legs, isLoop);
            selectedEditingPoint = selectedIndex;
            routingState = RoutingUiState.Idle;
            routingMessage = null;
            await CommitManualEditAsync();
            return;
        }

        routingCancellation?.Cancel();
        routingCancellation?.Dispose();
        routingCancellation = new CancellationTokenSource();
        CancellationToken cancellationToken = routingCancellation.Token;
        routingState = RoutingUiState.Routing;
        routingMessage = affected.Count == 1 ? "Calculating bicycle route…" : "Updating bicycle route legs…";
        await InvokeAsync(StateHasChanged);

        IReadOnlyList<RoutePoint>[] routedLegs = [.. legs];
        foreach (int legIndex in affected)
        {
            RoutePoint start = anchors[legIndex];
            RoutePoint finish = anchors[(legIndex + 1) % anchors.Count];
            RoutePlanResult result;
            try
            {
                result = await RoutePlanner.PlanAsync(
                    new RoutePlanRequest(new[] { start.Coordinate, finish.Coordinate }, routingProfile),
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (result.Status != RoutePlanStatus.Success)
            {
                routingState = result.Status == RoutePlanStatus.NoRoute
                    ? RoutingUiState.NoRoute
                    : RoutingUiState.Failure;
                routingMessage = result.Message;
                await InvokeAsync(StateHasChanged);
                return;
            }
            routedLegs[legIndex] = NormaliseLegEndpoints(result.Geometry, start, finish);
        }

        if (cancellationToken.IsCancellationRequested || !ReferenceEquals(manualEditor, editor)) return;
        editor.ApplyRoutedEdit(anchors, routedLegs, isLoop);
        selectedEditingPoint = selectedIndex;
        routingState = RoutingUiState.Success;
        routingMessage = "Bicycle route updated.";
        await CommitManualEditAsync();
    }

    private static (IReadOnlyList<IReadOnlyList<RoutePoint>> Legs, IReadOnlyList<int> Affected) BuildProposedLegs(
        IReadOnlyList<RoutePoint> previousAnchors,
        IReadOnlyList<IReadOnlyList<RoutePoint>> previousLegs,
        bool previousLoop,
        IReadOnlyList<RoutePoint> anchors,
        bool loop,
        bool rerouteAll)
    {
        int legCount = loop ? anchors.Count : Math.Max(0, anchors.Count - 1);
        var legs = new List<IReadOnlyList<RoutePoint>>(legCount);
        var affected = new List<int>();
        for (int index = 0; index < legCount; index++)
        {
            RoutePoint start = anchors[index];
            RoutePoint finish = anchors[(index + 1) % anchors.Count];
            int previousIndex = previousAnchors.ToList().FindIndex(anchor => anchor == start);
            int previousLegCount = previousLoop ? previousAnchors.Count : Math.Max(0, previousAnchors.Count - 1);
            bool preserved = !rerouteAll && previousIndex >= 0 && previousIndex < previousLegCount &&
                previousAnchors[(previousIndex + 1) % previousAnchors.Count] == finish;
            if (preserved)
            {
                legs.Add(previousLegs[previousIndex]);
            }
            else
            {
                legs.Add(Array.AsReadOnly(new[] { start, finish }));
                affected.Add(index);
            }
        }
        return (Array.AsReadOnly(legs.ToArray()), Array.AsReadOnly(affected.ToArray()));
    }

    private static IReadOnlyList<RoutePoint> NormaliseLegEndpoints(
        IReadOnlyList<RoutePoint> geometry,
        RoutePoint start,
        RoutePoint finish)
    {
        RoutePoint[] points = [.. geometry];
        points[0] = start;
        points[^1] = finish;
        return Array.AsReadOnly(points);
    }

    private async Task CommitManualEditAsync()
    {
        if (manualEditor is null || manualDocumentId is not { } documentId || manualTarget is not { } target) return;
        WorkspaceDocument current = workspace.Documents.Single(document => document.Id == documentId);
        RouteDocument changed = target.ReplacePoints(
            current.Document,
            manualEditor.RoutePoints,
            manualEditor.AnchorIndices);
        workspace = workspace.ReplaceDocument(documentId, changed);
        manualEditDirty = manualOriginalDocument is not null &&
            (!target.GetPoints(manualOriginalDocument).SequenceEqual(manualEditor.RoutePoints) ||
             !target.GetAnchorIndices(manualOriginalDocument).SequenceEqual(manualEditor.AnchorIndices));
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

    private enum RoutingUiState
    {
        Idle,
        Routing,
        Success,
        NoRoute,
        Failure
    }
}
