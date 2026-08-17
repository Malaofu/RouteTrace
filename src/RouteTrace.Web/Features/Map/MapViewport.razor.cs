using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace RouteTrace.Web.Features.Map;

public partial class MapViewport
{
    [Inject] private IJSRuntime JavaScript { get; set; } = null!;
    [Inject] private IOptions<MapMarkerOptions> MarkerOptions { get; set; } = null!;

    [Parameter]
    public IReadOnlyList<MapDocumentGeometry> Documents { get; set; } = [];

    [Parameter]
    public MapSelection Selection { get; set; }
    [Parameter] public int FocusVersion { get; set; }

    private readonly string elementId = $"route-map-{Guid.NewGuid():N}";
    private readonly Dictionary<Guid, MapDocumentGeometry> renderedDocuments = [];
    private IJSObjectReference? module;
    private DotNetObjectReference<MapViewport>? selfReference;
    private HoveredWaypoint? hoveredWaypoint;
    private int renderedFocusVersion;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        module = await JavaScript.InvokeAsync<IJSObjectReference>(
            "import",
            "./generated/mapAdapter.js");
        selfReference = DotNetObjectReference.Create(this);
        await module.InvokeVoidAsync("initialize", elementId, MarkerOptions.Value, selfReference);
        await RenderDocumentAsync(module);
    }

    [JSInvokable]
    public Task ShowWaypointTooltip(Guid documentId, int waypointIndex, double left, double top)
    {
        MapDocumentGeometry? document = Documents.FirstOrDefault(document => document.Id == documentId);
        hoveredWaypoint = document is not null && waypointIndex >= 0 && waypointIndex < document.Geometry.Waypoints.Count
            ? new(documentId, waypointIndex, document.Geometry.Waypoints[waypointIndex], left, top)
            : null;
        return InvokeAsync(StateHasChanged);
    }

    [JSInvokable]
    public Task HideWaypointTooltip()
    {
        if (hoveredWaypoint is null) return Task.CompletedTask;
        hoveredWaypoint = null;
        return InvokeAsync(StateHasChanged);
    }

    private static string WaypointMetadata(MapWaypoint waypoint)
    {
        string coordinates = FormattableString.Invariant($"{waypoint.Coordinate[1]:F6}° {waypoint.Coordinate[0]:F6}°");
        string metadata = $"{waypoint.Symbol ?? "Waypoint"} · {coordinates}";
        return waypoint.ElevationMetres is { } elevation ? $"{metadata} · {Math.Round(elevation)} m" : metadata;
    }

    private double HoveredPinHeight()
    {
        if (hoveredWaypoint is null) return 0;
        bool selected = Selection.DocumentId == hoveredWaypoint.DocumentId &&
            (Selection.WholeDocument || Selection.WaypointGroup || Selection.WaypointIndex == hoveredWaypoint.WaypointIndex);
        double scale = selected ? MarkerOptions.Value.SelectedPinScale : MarkerOptions.Value.PinScale;
        return MapMarkerOptions.PinIntrinsicHeightPixels * scale;
    }

    protected override async Task OnParametersSetAsync()
    {
        if (module is { } currentModule)
        {
            await RenderDocumentAsync(currentModule);
            if (FocusVersion != renderedFocusVersion)
            {
                renderedFocusVersion = FocusVersion;
                await currentModule.InvokeVoidAsync("focusSelection", elementId, Selection.DocumentId, Selection.TrackIndex, Selection.SegmentIndex, Selection.RouteIndex, Selection.WaypointIndex, Selection.WholeDocument, Selection.WaypointGroup);
            }
        }
    }

    private async Task RenderDocumentAsync(IJSObjectReference currentModule)
    {
        var current = Documents.ToDictionary(document => document.Id);

        foreach (Guid removedId in renderedDocuments.Keys.Except(current.Keys).ToArray())
        {
            await currentModule.InvokeVoidAsync("removeDocument", elementId, removedId);
        }

        foreach (MapDocumentGeometry document in Documents)
        {
            bool selectionTargetsDocument = Selection.DocumentId is null
                ? document.IsActive
                : document.Id == Selection.DocumentId;
            if (!renderedDocuments.TryGetValue(document.Id, out MapDocumentGeometry? previous)
                || !ReferenceEquals(previous.Geometry, document.Geometry))
            {
                await currentModule.InvokeVoidAsync(
                    "upsertDocument", elementId, document.Id, document.Geometry, document.Colour);
            }

            if (previous is null
                || previous.IsActive != document.IsActive
                || previous.IsSelected != document.IsSelected
                || previous.Colour != document.Colour
                || !previous.Presentation.SequenceEqual(document.Presentation)
                || (document.IsActive && renderedDocuments.Count > 0))
            {
                await currentModule.InvokeVoidAsync(
                    "setDocumentPresentation",
                    elementId,
                    document.Id,
                    document.Colour,
                    document.IsActive,
                    document.IsSelected,
                    document.Presentation,
                    selectionTargetsDocument ? Selection.TrackIndex : null,
                    selectionTargetsDocument ? Selection.SegmentIndex : null,
                    selectionTargetsDocument ? Selection.RouteIndex : null,
                    selectionTargetsDocument ? Selection.WaypointIndex : null,
                    selectionTargetsDocument && Selection.WholeDocument,
                    selectionTargetsDocument && Selection.WaypointGroup);
            }
        }

        bool geometryChanged = renderedDocuments.Keys.ToHashSet().SetEquals(current.Keys) is false
            || Documents.Any(document => !renderedDocuments.TryGetValue(document.Id, out MapDocumentGeometry? previous)
                || !ReferenceEquals(previous.Geometry, document.Geometry));
        renderedDocuments.Clear();
        foreach ((Guid id, MapDocumentGeometry document) in current) renderedDocuments[id] = document;
        await currentModule.InvokeVoidAsync("endDocumentUpdate", elementId, geometryChanged);
    }

    public async ValueTask DisposeAsync()
    {
        if (module is null)
        {
            return;
        }

        try
        {
            await module.InvokeVoidAsync("dispose", elementId);
            await module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
            // The browser has already disconnected, so there is nothing to clean up.
        }
        finally
        {
            selfReference?.Dispose();
        }
    }

    private sealed record HoveredWaypoint(Guid DocumentId, int WaypointIndex, MapWaypoint Waypoint, double Left, double Top);
}
