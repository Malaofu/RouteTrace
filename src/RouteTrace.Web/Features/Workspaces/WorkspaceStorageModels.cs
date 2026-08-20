using System.Text.Json.Serialization;
using RouteTrace.Core.Routes.Workspaces;
using RouteTrace.Core.Editing;

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
    [property: JsonPropertyName("presentationOverrides")] IReadOnlyList<NodePresentationStorageDto>? PresentationOverrides,
    [property: JsonPropertyName("gpx")] string Gpx,
    [property: JsonPropertyName("editingAnchors")] IReadOnlyList<LineAnchorStorageDto>? EditingAnchors = null);

public sealed record LineAnchorStorageDto(
    [property: JsonPropertyName("kind")] EditableLineKind Kind,
    [property: JsonPropertyName("primaryIndex")] int PrimaryIndex,
    [property: JsonPropertyName("secondaryIndex")] int SecondaryIndex,
    [property: JsonPropertyName("pointIndices")] IReadOnlyList<int> PointIndices);

public sealed record NodePresentationStorageDto(
    [property: JsonPropertyName("kind")] WorkspaceNodeKind Kind,
    [property: JsonPropertyName("primaryIndex")] int PrimaryIndex,
    [property: JsonPropertyName("secondaryIndex")] int SecondaryIndex,
    [property: JsonPropertyName("isVisible")] bool? IsVisible,
    [property: JsonPropertyName("colour")] string? Colour);

public sealed record SavedWorkspaceSummary(Guid Id, string Name);

public sealed record StoredWorkspaceRecord(Guid Id, string Name, string Payload);
