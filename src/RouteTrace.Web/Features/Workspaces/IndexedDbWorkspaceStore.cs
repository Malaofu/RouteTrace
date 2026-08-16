using Microsoft.JSInterop;
using RouteTrace.Core.Routes.Workspaces;

namespace RouteTrace.Web.Features.Workspaces;

public sealed class IndexedDbWorkspaceStore(IJSRuntime javaScript) : IWorkspaceStore, IAsyncDisposable
{
    private IJSObjectReference? module;
    private readonly SemaphoreSlim saveLock = new(1, 1);
    private long requestedSaveRevision;

    public async Task<IReadOnlyList<SavedWorkspaceSummary>> ListAsync(CancellationToken cancellationToken = default)
    {
        IJSObjectReference storage = await GetModuleAsync(cancellationToken);
        return await storage.InvokeAsync<SavedWorkspaceSummary[]>("listWorkspaces", cancellationToken) ?? [];
    }

    public async Task SaveAsync(RouteWorkspace workspace, CancellationToken cancellationToken = default)
    {
        long revision = Interlocked.Increment(ref requestedSaveRevision);
        await saveLock.WaitAsync(cancellationToken);
        try
        {
            if (revision < Volatile.Read(ref requestedSaveRevision)) return;
            string payload = await WorkspaceCodec.EncodeAsync(workspace, cancellationToken);
            if (revision < Volatile.Read(ref requestedSaveRevision)) return;
            IJSObjectReference storage = await GetModuleAsync(cancellationToken);
            await storage.InvokeVoidAsync("saveWorkspace", cancellationToken, new StoredWorkspaceRecord(workspace.Id, workspace.Name, payload));
        }
        finally
        {
            saveLock.Release();
        }
    }

    public async Task<WorkspaceDecodeResult> OpenAsync(Guid id, CancellationToken cancellationToken = default)
    {
        IJSObjectReference storage = await GetModuleAsync(cancellationToken);
        string? payload = await storage.InvokeAsync<string?>("openWorkspace", cancellationToken, id);
        return payload is null
            ? WorkspaceDecodeResult.Failure("The saved workspace no longer exists.")
            : await WorkspaceCodec.DecodeAsync(payload, cancellationToken);
    }

    public async Task<WorkspaceDecodeResult?> OpenMostRecentAsync(CancellationToken cancellationToken = default)
    {
        IJSObjectReference storage = await GetModuleAsync(cancellationToken);
        string? payload = await storage.InvokeAsync<string?>("openMostRecentWorkspace", cancellationToken);
        return payload is null ? null : await WorkspaceCodec.DecodeAsync(payload, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        IJSObjectReference storage = await GetModuleAsync(cancellationToken);
        await storage.InvokeVoidAsync("deleteWorkspace", cancellationToken, id);
    }

    private async Task<IJSObjectReference> GetModuleAsync(CancellationToken cancellationToken) =>
        module ??= await javaScript.InvokeAsync<IJSObjectReference>("import", cancellationToken, "./generated/workspaceStorage.js");

    public async ValueTask DisposeAsync()
    {
        if (module is not null) await module.DisposeAsync();
        saveLock.Dispose();
    }
}
