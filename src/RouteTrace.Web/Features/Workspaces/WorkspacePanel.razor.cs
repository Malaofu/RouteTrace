using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using RouteTrace.Core.Routes;

namespace RouteTrace.Web.Features.Workspaces;

public partial class WorkspacePanel
{
    [Inject] private IWorkspaceStore Store { get; set; } = null!;
    [Inject] private IJSRuntime JavaScript { get; set; } = null!;

    [Parameter, EditorRequired]
    public required RouteWorkspace Workspace { get; set; }

    [Parameter]
    public EventCallback<RouteWorkspace> WorkspaceChanged { get; set; }

    [Parameter]
    public EventCallback<Guid> WorkspaceDeleted { get; set; }

    private IReadOnlyList<SavedWorkspaceSummary> savedWorkspaces = [];
    private string workspaceName = string.Empty;
    private string? message;
    private string? error;
    private bool isBusy;

    protected override async Task OnInitializedAsync()
    {
        workspaceName = Workspace.Name;
        await RefreshAsync();
    }

    protected override void OnParametersSet()
    {
        if (workspaceName != Workspace.Name && !isBusy) workspaceName = Workspace.Name;
    }

    private async Task RenameAsync(ChangeEventArgs eventArgs)
    {
        string newName = eventArgs.Value?.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(newName))
        {
            error = "The workspace name cannot be empty.";
            return;
        }

        workspaceName = newName.Trim();
        await WorkspaceChanged.InvokeAsync(Workspace.Rename(workspaceName));
        await RefreshAsync();
        message = $"Renamed to {workspaceName}.";
    }

    private async Task OpenAsync(Guid id)
    {
        await RunAsync(async () =>
        {
            WorkspaceDecodeResult result = await Store.OpenAsync(id);
            if (result.Workspace is not { } openedWorkspace)
            {
                error = result.Error;
                return;
            }
            workspaceName = openedWorkspace.Name;
            await WorkspaceChanged.InvokeAsync(openedWorkspace);
            message = $"Opened {openedWorkspace.Name}.";
        });
    }

    private async Task DeleteAsync(SavedWorkspaceSummary saved)
    {
        bool confirmed = await JavaScript.InvokeAsync<bool>("confirm", $"Delete the saved workspace '{saved.Name}'?");
        if (!confirmed) return;

        await RunAsync(async () =>
        {
            await Store.DeleteAsync(saved.Id);
            await WorkspaceDeleted.InvokeAsync(saved.Id);
            await RefreshAsync();
            message = $"Deleted {saved.Name}.";
        });
    }

    private async Task RefreshAsync() => savedWorkspaces = await Store.ListAsync();

    private async Task RunAsync(Func<Task> action)
    {
        isBusy = true;
        error = null;
        message = null;
        try
        {
            await action();
        }
        catch (JSException)
        {
            error = "Browser storage is unavailable.";
        }
        finally
        {
            isBusy = false;
        }
    }
}
