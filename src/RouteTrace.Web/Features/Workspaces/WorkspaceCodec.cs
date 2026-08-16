using System.Text;
using System.Text.Json;
using System.Runtime.CompilerServices;
using RouteTrace.Core.Gpx;
using RouteTrace.Core.Routes;

namespace RouteTrace.Web.Features.Workspaces;

public static class WorkspaceCodec
{
    public const int CurrentSchemaVersion = 3;
    private static readonly ConditionalWeakTable<RouteDocument, CachedGpx> EncodedDocuments = new();

    public static async Task<string> EncodeAsync(RouteWorkspace workspace, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        var documents = new List<WorkspaceDocumentStorageDto>(workspace.Documents.Count);
        foreach (WorkspaceDocument document in workspace.Documents)
        {
            documents.Add(new WorkspaceDocumentStorageDto(
                document.Id,
                document.SourceFileName,
                document.IsVisible,
                document.Colour,
                document.PresentationOverrides.Values.Select(item => new NodePresentationStorageDto(item.Node.Kind, item.Node.PrimaryIndex, item.Node.SecondaryIndex, item.IsVisible, item.Colour)).ToArray(),
                await EncodeDocumentAsync(document.Document, cancellationToken)));
        }

        return JsonSerializer.Serialize(new WorkspaceStorageDto(
            CurrentSchemaVersion, workspace.Id, workspace.Name, workspace.ActiveDocumentId, workspace.SelectedDocumentId, documents));
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
        if (dto.SchemaVersion is not (1 or 2 or CurrentSchemaVersion))
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
                if (imported.Document is not { } routeDocument)
                    return WorkspaceDecodeResult.Failure($"A saved workspace document is invalid: {imported.Error}");
                documents.Add(new WorkspaceDocument(
                    storedDocument.Id,
                    routeDocument,
                    storedDocument.SourceFileName,
                    storedDocument.IsVisible ?? true,
                    storedDocument.Colour ?? WorkspaceDocument.DefaultColour(documents.Count),
                    storedDocument.PresentationOverrides?.Select(item => new NodePresentationOverride(new WorkspaceNode(item.Kind, item.PrimaryIndex, item.SecondaryIndex), item.IsVisible, item.Colour))));
            }

            return WorkspaceDecodeResult.Success(new RouteWorkspace(
                dto.Id, dto.Name, documents, dto.ActiveDocumentId, dto.SelectedDocumentId));
        }
        catch (ArgumentException)
        {
            return WorkspaceDecodeResult.Failure("The saved workspace contains invalid identifiers or state.");
        }
    }

    private static async Task<string> EncodeDocumentAsync(RouteDocument document, CancellationToken cancellationToken)
    {
        if (EncodedDocuments.TryGetValue(document, out CachedGpx? cached)) return cached.Value;

        await using var stream = new MemoryStream();
        await GpxExporter.ExportAsync(document, stream, "Route Trace", cancellationToken);
        string value = Encoding.UTF8.GetString(stream.ToArray());
        try
        {
            EncodedDocuments.Add(document, new CachedGpx(value));
        }
        catch (ArgumentException)
        {
            return EncodedDocuments.GetValue(document, _ => new CachedGpx(value)).Value;
        }
        return value;
    }

    private sealed record CachedGpx(string Value);
}

public sealed record WorkspaceDecodeResult(RouteWorkspace? Workspace, string? Error)
{
    public bool IsSuccess => Workspace is not null;
    public static WorkspaceDecodeResult Success(RouteWorkspace workspace) => new(workspace, null);
    public static WorkspaceDecodeResult Failure(string error) => new(null, error);
}
