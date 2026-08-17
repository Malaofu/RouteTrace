using Microsoft.AspNetCore.Components.Forms;
using RouteTrace.Core.Gpx;

namespace RouteTrace.Web.Features.Import;

public sealed class GpxImportOperation
{
    public const long MaximumFileSize = 50 * 1024 * 1024; // 50 Mb

    public async Task<GpxImportOutcome> ExecuteAsync(
        IBrowserFile file,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using Stream stream = file.OpenReadStream(MaximumFileSize, cancellationToken);
            GpxImportResult result = await GpxImporter.ImportAsync(stream, cancellationToken);
            if (result.Document is not { } document)
            {
                return GpxImportOutcome.Failed(result.Error);
            }

            int pointCount = document.Tracks
                .SelectMany(track => track.Segments)
                .Sum(segment => segment.Points.Count)
                + document.Routes.Sum(route => route.Points.Count)
                + document.Waypoints.Count;
            var imported = new ImportedGpxDocument(document, file.Name);
            return GpxImportOutcome.Succeeded(
                imported,
                $"Imported {file.Name}: {pointCount} point(s).");
        }
        catch (IOException exception)
        {
            return GpxImportOutcome.Failed($"The file could not be read: {exception.Message}");
        }
    }
}

public sealed record GpxImportOutcome(
    ImportedGpxDocument? ImportedDocument,
    string? Message,
    bool IsError)
{
    public static GpxImportOutcome Succeeded(ImportedGpxDocument document, string message) =>
        new(document, message, false);

    public static GpxImportOutcome Failed(string? message) =>
        new(null, message, true);
}
