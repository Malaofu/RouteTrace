using Microsoft.JSInterop;
using RouteTrace.Core.Gpx;
using RouteTrace.Core.Routes.Workspaces;

namespace RouteTrace.Web.Features.Import;

public sealed class GpxExportOperation(IJSRuntime javaScript) : IAsyncDisposable
{
    private IJSObjectReference? downloadModule;

    public async Task<GpxExportOutcome> ExecuteAsync(
        WorkspaceDocument target,
        CancellationToken cancellationToken = default)
    {
        await using var stream = new MemoryStream();
        GpxExportResult result = await GpxExporter.ExportAsync(
            target.Document,
            stream,
            "Route Trace",
            cancellationToken);
        stream.Position = 0;

        using var streamReference = new DotNetStreamReference(stream);
        downloadModule ??= await javaScript.InvokeAsync<IJSObjectReference>("import", cancellationToken, "./generated/download.js");
        string fileName = GpxDownloadFileName.From(
            target.Document.Metadata?.Name,
            target.SourceFileName);
        await downloadModule.InvokeVoidAsync(
            "downloadStream",
            cancellationToken,
            fileName,
            "application/gpx+xml",
            streamReference);
        return GpxExportOutcome.From(result);
    }

    public async ValueTask DisposeAsync()
    {
        if (downloadModule is not null)
        {
            await downloadModule.DisposeAsync();
        }
    }
}

public sealed record GpxExportOutcome(string Notice)
{
    public static GpxExportOutcome From(GpxExportResult result) => new(
        result.OmittedExtensionNamespaces.Count == 0
            ? $"Downloaded GPX with {result.RetainedExtensionCount} retained extension element(s)."
            : $"Downloaded GPX; omitted: {string.Join(", ", result.OmittedExtensionNamespaces)}.");
}
