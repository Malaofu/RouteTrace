using System.Text.Json;
using RouteTrace.Core.Routes;
using RouteTrace.Web.Features.Workspaces;

namespace RouteTrace.Web.Tests;

public sealed class WorkspaceCodecTests
{
    [Fact]
    public async Task RoundTripsMultipleDocumentsStableIdsAndActiveDocument()
    {
        var first = new WorkspaceDocument(
            Guid.NewGuid(),
            new RouteDocument(metadata: new RouteMetadata(name: "First")),
            "first.gpx");
        var second = new WorkspaceDocument(
            Guid.NewGuid(),
            new RouteDocument(metadata: new RouteMetadata(name: "Second")),
            "second.gpx");
        var workspace = new RouteWorkspace(Guid.NewGuid(), "Saved routes", [first, second], second.Id, first.Id);

        string payload = await WorkspaceCodec.EncodeAsync(workspace, TestContext.Current.CancellationToken);
        WorkspaceDecodeResult decoded = await WorkspaceCodec.DecodeAsync(payload, TestContext.Current.CancellationToken);

        decoded.IsSuccess.ShouldBeTrue();
        decoded.Workspace!.Id.ShouldBe(workspace.Id);
        decoded.Workspace.Name.ShouldBe("Saved routes");
        decoded.Workspace.Documents.Select(document => document.Id).ShouldBe([first.Id, second.Id]);
        decoded.Workspace.Documents.Select(document => document.Document.Metadata?.Name).ShouldBe(["First", "Second"]);
        decoded.Workspace.ActiveDocumentId.ShouldBe(second.Id);
        decoded.Workspace.SelectedDocumentId.ShouldBe(first.Id);
        decoded.Workspace.ActiveDocument!.SourceFileName.ShouldBe("second.gpx");
    }

    [Fact]
    public async Task RejectsAnUnsupportedSchemaVersion()
    {
        string payload = JsonSerializer.Serialize(new WorkspaceStorageDto(
            99, Guid.NewGuid(), "Future", null, null, []));

        WorkspaceDecodeResult result = await WorkspaceCodec.DecodeAsync(payload, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("Workspace schema version 99 is not supported.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    public async Task RejectsEmptyOrCorruptStorage(string payload)
    {
        WorkspaceDecodeResult result = await WorkspaceCodec.DecodeAsync(payload, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNullOrWhiteSpace();
    }
}
