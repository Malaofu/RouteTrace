using System.Text;
using System.Text.Json;
using RouteTrace.Core.Gpx;
using RouteTrace.Core.Routes;

namespace RouteTrace.Web.Features.Workspaces;

public static class WorkspaceCodec
{
    public const int CurrentSchemaVersion = 1;

    public static async Task<string> EncodeAsync(RouteWorkspace workspace, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        var documents = new List<WorkspaceDocumentStorageDto>(workspace.Documents.Count);
        foreach (WorkspaceDocument document in workspace.Documents)
        {
            await using var stream = new MemoryStream();
            await GpxExporter.ExportAsync(document.Document, stream, "Route Trace", cancellationToken);
            documents.Add(new WorkspaceDocumentStorageDto(
                document.Id,
                document.SourceFileName,
                Encoding.UTF8.GetString(stream.ToArray())));
        }

        return JsonSerializer.Serialize(new WorkspaceStorageDto(
            CurrentSchemaVersion, workspace.Id, workspace.Name, workspace.ActiveDocumentId, documents));
    }

    public static async Task<WorkspaceDecodeResult> DecodeAsync(string payload, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(payload)) return WorkspaceDecodeResult.Failure("The saved workspace is empty.");

        WorkspaceStorageDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<WorkspaceStorageDto>(payload);
        }
        catch (JsonException)
        {
            return WorkspaceDecodeResult.Failure("The saved workspace is corrupt.");
        }

        if (dto is null) return WorkspaceDecodeResult.Failure("The saved workspace is corrupt.");
        if (dto.SchemaVersion != CurrentSchemaVersion)
            return WorkspaceDecodeResult.Failure($"Workspace schema version {dto.SchemaVersion} is not supported.");
        if (dto.Documents is null) return WorkspaceDecodeResult.Failure("The saved workspace has no document collection.");

        try
        {
            var documents = new List<WorkspaceDocument>(dto.Documents.Count);
            foreach (WorkspaceDocumentStorageDto storedDocument in dto.Documents)
            {
                if (storedDocument is null || string.IsNullOrWhiteSpace(storedDocument.Gpx))
                    return WorkspaceDecodeResult.Failure("A saved workspace document is corrupt.");

                await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(storedDocument.Gpx));
                GpxImportResult imported = await GpxImporter.ImportAsync(stream, cancellationToken);
                if (!imported.IsSuccess)
                    return WorkspaceDecodeResult.Failure($"A saved workspace document is invalid: {imported.Error}");
                documents.Add(new WorkspaceDocument(storedDocument.Id, imported.Document!, storedDocument.SourceFileName));
            }

            return WorkspaceDecodeResult.Success(new RouteWorkspace(dto.Id, dto.Name, documents, dto.ActiveDocumentId));
        }
        catch (ArgumentException)
        {
            return WorkspaceDecodeResult.Failure("The saved workspace contains invalid identifiers or state.");
        }
    }
}

public sealed record WorkspaceDecodeResult(RouteWorkspace? Workspace, string? Error)
{
    public bool IsSuccess => Workspace is not null;
    public static WorkspaceDecodeResult Success(RouteWorkspace workspace) => new(workspace, null);
    public static WorkspaceDecodeResult Failure(string error) => new(null, error);
}
