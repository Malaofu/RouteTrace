using System.Text.Json.Serialization;

namespace RouteTrace.Web.Features.Workspaces;

public sealed record WorkspaceStorageDto(
    [property: JsonPropertyName("schemaVersion")] int SchemaVersion,
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("activeDocumentId")] Guid? ActiveDocumentId,
    [property: JsonPropertyName("selectedDocumentId")] Guid? SelectedDocumentId,
    [property: JsonPropertyName("documents")] IReadOnlyList<WorkspaceDocumentStorageDto> Documents);

public sealed record WorkspaceDocumentStorageDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("sourceFileName")] string? SourceFileName,
    [property: JsonPropertyName("isVisible")] bool? IsVisible,
    [property: JsonPropertyName("colour")] string? Colour,
    [property: JsonPropertyName("gpx")] string Gpx);

public sealed record SavedWorkspaceSummary(Guid Id, string Name);

public sealed record StoredWorkspaceRecord(Guid Id, string Name, string Payload);
