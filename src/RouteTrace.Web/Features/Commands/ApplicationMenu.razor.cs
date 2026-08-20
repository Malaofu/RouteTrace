using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using RouteTrace.Core.Routes.Workspaces;
using RouteTrace.Web.Features.Import;

namespace RouteTrace.Web.Features.Commands;

public partial class ApplicationMenu
{
    [Inject] private IJSRuntime JavaScript { get; set; } = null!;
    [Inject] private GpxImportOperation GpxImport { get; set; } = null!;
    [Inject] private GpxExportOperation GpxExport { get; set; } = null!;

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
    [Parameter] public bool EditingActive { get; set; }
    [Parameter] public EventCallback EditUndoRequested { get; set; }
    [Parameter] public EventCallback EditRedoRequested { get; set; }
    [Parameter] public EventCallback EditCloseRequested { get; set; }

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
    private bool dropTargetVisible;
    private bool isImporting;
    private bool hasError;
    private string? message;
    private CancellationTokenSource? noticeDismissal;
    private IJSObjectReference? menuModule;
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
            if (fileInput is { } input)
            {
                await menuModule.InvokeVoidAsync("attachApplicationMenu", selfReference, input.Element);
            }
        }

        if (menuModule is not null)
        {
            await menuModule.InvokeVoidAsync("setEditingActive", EditingActive);
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
        "editUndo" when EditingActive => EditUndoRequested.InvokeAsync(),
        "editRedo" when EditingActive => EditRedoRequested.InvokeAsync(),
        "editClose" when EditingActive => EditCloseRequested.InvokeAsync(),
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

    [JSInvokable]
    public Task SetDropTargetVisible(bool visible)
    {
        if (dropTargetVisible == visible)
        {
            return Task.CompletedTask;
        }

        dropTargetVisible = visible;
        return InvokeAsync(StateHasChanged);
    }

    private async Task ImportAsync(InputFileChangeEventArgs eventArgs)
    {
        isImporting = true;
        hasError = false;
        message = null;
        CancelNoticeDismissal();
        await InvokeAsync(StateHasChanged);
        await JavaScript.InvokeVoidAsync("routeTrace.waitForAnimationFrame");
        try
        {
            IBrowserFile file = eventArgs.File;
            GpxImportOutcome outcome = await GpxImport.ExecuteAsync(file);
            hasError = outcome.IsError;
            message = outcome.Message;
            if (outcome.ImportedDocument is not { } importedDocument)
            {
                return;
            }

            await DocumentImported.InvokeAsync(importedDocument);
            ScheduleNoticeDismissal();
        }
        finally
        {
            isImporting = false;
        }
    }

    private async Task ExportCoreAsync()
    {
        if (ActiveDocument is null) return;

        GpxExportOutcome outcome = await GpxExport.ExecuteAsync(ActiveDocument);
        message = outcome.Notice;
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
        selfReference?.Dispose();
    }
}
