using RouteTrace.Core.Routes.Workspaces;

namespace RouteTrace.Web.Features.Workspaces;

public interface IWorkspaceStore
{
    Task<IReadOnlyList<SavedWorkspaceSummary>> ListAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(RouteWorkspace workspace, CancellationToken cancellationToken = default);
    Task<WorkspaceDecodeResult> OpenAsync(Guid id, CancellationToken cancellationToken = default);
    Task<WorkspaceDecodeResult?> OpenMostRecentAsync(CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
