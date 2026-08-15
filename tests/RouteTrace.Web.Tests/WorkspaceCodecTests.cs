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

    [Fact]
    public async Task RoundTripsPresentationOverridesOutsideGpx()
    {
        var document = new WorkspaceDocument(Guid.NewGuid(), new RouteDocument(), "route.gpx")
            .WithNodeVisibility(new WorkspaceNode(WorkspaceNodeKind.Route, 0), false)
            .WithNodeColour(new WorkspaceNode(WorkspaceNodeKind.Route, 0), "#123456");
        var workspace = new RouteWorkspace(Guid.NewGuid(), "Presentation", [document], document.Id);

        string payload = await WorkspaceCodec.EncodeAsync(workspace, TestContext.Current.CancellationToken);
        WorkspaceDecodeResult decoded = await WorkspaceCodec.DecodeAsync(payload, TestContext.Current.CancellationToken);

        WorkspaceDocument restored = decoded.Workspace!.Documents.Single();
        restored.IsNodeVisible(new WorkspaceNode(WorkspaceNodeKind.Route, 0)).ShouldBeFalse();
        restored.NodeColour(new WorkspaceNode(WorkspaceNodeKind.Route, 0)).ShouldBe("#123456");
        payload.ShouldContain("presentationOverrides");
        payload.ShouldNotContain("<extensions>");
    }

    [Fact]
    public async Task RoundTripsEditedDocumentAndChildInfo()
    {
        var original = new RouteDocument(
            tracks: [new Track("Before", [new TrackSegment([])])],
            metadata: new RouteMetadata("Original", "Original description"));
        var document = new WorkspaceDocument(Guid.NewGuid(), original, "route.gpx");
        RouteWorkspace workspace = new RouteWorkspace(Guid.NewGuid(), "Info", [document], document.Id);
        await WorkspaceCodec.EncodeAsync(workspace, TestContext.Current.CancellationToken);
        workspace = workspace
            .UpdateNodeInfo(document.Id, null, "Renamed", "Updated description")
            .UpdateNodeInfo(document.Id, new WorkspaceNode(WorkspaceNodeKind.Track, 0), "Renamed track", null);

        string payload = await WorkspaceCodec.EncodeAsync(workspace, TestContext.Current.CancellationToken);
        WorkspaceDecodeResult decoded = await WorkspaceCodec.DecodeAsync(payload, TestContext.Current.CancellationToken);

        decoded.Workspace!.Documents[0].Document.Metadata!.Name.ShouldBe("Renamed");
        decoded.Workspace.Documents[0].Document.Metadata!.Description.ShouldBe("Updated description");
        decoded.Workspace.Documents[0].Document.Tracks[0].Name.ShouldBe("Renamed track");
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
