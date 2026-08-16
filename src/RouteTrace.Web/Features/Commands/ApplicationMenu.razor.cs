using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using RouteTrace.Core.Gpx;
using RouteTrace.Core.Routes.Documents;
using RouteTrace.Core.Routes.Workspaces;
using RouteTrace.Web.Features.Import;

namespace RouteTrace.Web.Features.Commands;

public partial class ApplicationMenu
{
    [Inject] private IJSRuntime JavaScript { get; set; } = null!;

    [Parameter, EditorRequired]
    public WorkspaceDocument? ActiveDocument { get; set; }

    [Parameter, EditorRequired]
    public EventCallback<ImportedGpxDocument> DocumentImported { get; set; }

    [Parameter]
    public bool InspectorVisible { get; set; }

    [Parameter]
    public EventCallback<bool> InspectorVisibilityChanged { get; set; }

    [Parameter] public bool ExplorerVisible { get; set; }
    [Parameter] public EventCallback<bool> ExplorerVisibilityChanged { get; set; }

    private const long MaximumFileSize = 50 * 1024 * 1024;
    private readonly ApplicationCommand openCommand;
    private readonly ApplicationCommand exportCommand;
    private readonly ApplicationCommand inspectorCommand;
    private readonly ApplicationCommand explorerCommand;
    private ElementReference menuButton;
    private ElementReference openButton;
    private ElementReference saveButton;
    private ElementReference viewMenuButton;
    private ElementReference inspectorButton;
    private ElementReference explorerButton;
    private InputFile? fileInput;
    private string? activeMenu;
    private string? focusMenu;
    private bool focusFirstItem;
    private bool isImporting;
    private bool hasError;
    private string? message;
    private CancellationTokenSource? noticeDismissal;
    private IJSObjectReference? menuModule;
    private IJSObjectReference? downloadModule;
    private DotNetObjectReference<ApplicationMenu>? selfReference;

    public ApplicationMenu()
    {
        openCommand = new(() => !isImporting, OpenFilePickerCoreAsync);
        exportCommand = new(() => ActiveDocument is not null && !isImporting, ExportCoreAsync);
        inspectorCommand = new(() => true, ToggleInspectorCoreAsync);
        explorerCommand = new(() => true, ToggleExplorerCoreAsync);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            menuModule = await JavaScript.InvokeAsync<IJSObjectReference>("import", "./generated/applicationMenu.js");
            selfReference = DotNetObjectReference.Create(this);
            await menuModule.InvokeVoidAsync("attachApplicationMenu", selfReference);
        }

        if (focusFirstItem)
        {
            focusFirstItem = false;
            await (focusMenu == "view" ? explorerButton : openButton).FocusAsync();
        }
    }

    private void ToggleMenu(string menu)
    {
        activeMenu = activeMenu == menu ? null : menu;
        if (activeMenu is not null)
        {
            focusMenu = activeMenu;
            focusFirstItem = true;
        }
    }

    private void ToggleFileMenu() => ToggleMenu("file");

    private void ToggleViewMenu() => ToggleMenu("view");

    private async Task OpenFilePickerAsync()
    {
        activeMenu = null;
        await openCommand.TryExecuteAsync();
    }

    private async Task OpenFilePickerCoreAsync()
    {
        if (menuModule is not null && fileInput is not null)
        {
            await menuModule.InvokeVoidAsync("openFilePicker", fileInput.Element);
        }
    }

    private async Task ExportFromMenuAsync()
    {
        activeMenu = null;
        await exportCommand.TryExecuteAsync();
    }

    private async Task ToggleInspectorAsync()
    {
        activeMenu = null;
        await inspectorCommand.TryExecuteAsync();
    }

    private Task ToggleInspectorCoreAsync() => InspectorVisibilityChanged.InvokeAsync(!InspectorVisible);

    private async Task ToggleExplorerAsync()
    {
        activeMenu = null;
        await explorerCommand.TryExecuteAsync();
    }

    private Task ToggleExplorerCoreAsync() => ExplorerVisibilityChanged.InvokeAsync(!ExplorerVisible);

    private async Task HandleMenuKeyDown(KeyboardEventArgs eventArgs)
    {
        if (activeMenu is null) return;

        switch (eventArgs.Key)
        {
            case "Escape":
                string closingMenu = activeMenu;
                activeMenu = null;
                await (closingMenu == "view" ? viewMenuButton : menuButton).FocusAsync();
                break;
            case "ArrowDown":
                await (activeMenu == "view" ? inspectorButton : exportCommand.CanExecute ? saveButton : openButton).FocusAsync();
                break;
            case "ArrowUp":
                await (activeMenu == "view" ? explorerButton : openButton).FocusAsync();
                break;
        }
    }

    [JSInvokable]
    public Task RunShortcutAsync(string command) => command switch
    {
        "open" => openCommand.TryExecuteAsync(),
        "export" => exportCommand.TryExecuteAsync(),
        "inspector" => inspectorCommand.TryExecuteAsync(),
        "explorer" => explorerCommand.TryExecuteAsync(),
        _ => Task.CompletedTask
    };

    [JSInvokable]
    public void DismissMenu()
    {
        if (activeMenu is not null)
        {
            activeMenu = null;
            StateHasChanged();
        }
    }

    private async Task ImportAsync(InputFileChangeEventArgs eventArgs)
    {
        await JavaScript.InvokeVoidAsync("performance.clearMarks");
        await JavaScript.InvokeVoidAsync("performance.mark", "routeTrace.import.start");
        isImporting = true;
        hasError = false;
        message = null;
        CancelNoticeDismissal();
        await InvokeAsync(StateHasChanged);
        await JavaScript.InvokeVoidAsync("routeTrace.waitForAnimationFrame");
        await JavaScript.InvokeVoidAsync("performance.mark", "routeTrace.import.busy-rendered");

        try
        {
            IBrowserFile file = eventArgs.File;
            await using Stream stream = file.OpenReadStream(MaximumFileSize);
            GpxImportResult result = await GpxImporter.ImportAsync(stream);
            await JavaScript.InvokeVoidAsync("performance.mark", "routeTrace.import.parsed");
            if (result.Document is not { } document)
            {
                hasError = true;
                message = result.Error;
                return;
            }

            int pointCount = document.Tracks.SelectMany(track => track.Segments).Sum(segment => segment.Points.Count)
                + document.Routes.Sum(route => route.Points.Count)
                + document.Waypoints.Count;
            message = $"Imported {file.Name}: {pointCount} point(s).";
            await DocumentImported.InvokeAsync(new ImportedGpxDocument(document, file.Name));
            ScheduleNoticeDismissal();
        }
        catch (IOException exception)
        {
            hasError = true;
            message = $"The file could not be read: {exception.Message}";
        }
        finally
        {
            isImporting = false;
        }
    }

    private async Task ExportCoreAsync()
    {
        if (ActiveDocument is null) return;

        await JavaScript.InvokeVoidAsync("performance.clearMarks", "routeTrace.export.start");
        await JavaScript.InvokeVoidAsync("performance.clearMarks", "routeTrace.export.serialized");
        await JavaScript.InvokeVoidAsync("performance.clearMarks", "routeTrace.export.downloaded");
        await JavaScript.InvokeVoidAsync("performance.mark", "routeTrace.export.start");
        await using var stream = new MemoryStream();
        GpxExportResult result = await GpxExporter.ExportAsync(ActiveDocument.Document, stream, "Route Trace");
        await JavaScript.InvokeVoidAsync("performance.mark", "routeTrace.export.serialized");
        stream.Position = 0;
        using var streamReference = new DotNetStreamReference(stream);
        downloadModule ??= await JavaScript.InvokeAsync<IJSObjectReference>("import", "./generated/download.js");
        string fileName = GpxDownloadFileName.From(ActiveDocument.Document.Metadata?.Name, ActiveDocument.SourceFileName);
        await downloadModule.InvokeVoidAsync("downloadStream", fileName, "application/gpx+xml", streamReference);
        await JavaScript.InvokeVoidAsync("performance.mark", "routeTrace.export.downloaded");
        message = result.OmittedExtensionNamespaces.Count == 0
            ? $"Downloaded GPX with {result.RetainedExtensionCount} retained extension element(s)."
            : $"Downloaded GPX; omitted: {string.Join(", ", result.OmittedExtensionNamespaces)}.";
        ScheduleNoticeDismissal();
    }

    private void ScheduleNoticeDismissal()
    {
        CancelNoticeDismissal();
        noticeDismissal = new CancellationTokenSource();
        _ = DismissNoticeAfterDelayAsync(noticeDismissal);
    }

    private async Task DismissNoticeAfterDelayAsync(CancellationTokenSource dismissal)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(5), dismissal.Token);
            if (ReferenceEquals(noticeDismissal, dismissal))
            {
                message = null;
                await InvokeAsync(StateHasChanged);
            }
        }
        catch (OperationCanceledException)
        {
            // A newer notification replaced this one, or the component was disposed.
        }
    }

    private void CancelNoticeDismissal()
    {
        noticeDismissal?.Cancel();
        noticeDismissal?.Dispose();
        noticeDismissal = null;
    }

    public async ValueTask DisposeAsync()
    {
        CancelNoticeDismissal();
        if (menuModule is not null)
        {
            await menuModule.InvokeVoidAsync("detachApplicationMenu");
            await menuModule.DisposeAsync();
        }
        if (downloadModule is not null) await downloadModule.DisposeAsync();
        selfReference?.Dispose();
    }
}
