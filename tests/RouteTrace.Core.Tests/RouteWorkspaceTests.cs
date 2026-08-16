using RouteTrace.Core.Routes.Documents;
using RouteTrace.Core.Routes.Workspaces;

namespace RouteTrace.Core.Tests;

public sealed class RouteWorkspaceTests
{
    [Fact]
    public void AddsDocumentsWithStableUniqueIdsAndActivatesTheNewest()
    {
        var workspace = new RouteWorkspace(Guid.NewGuid(), "Weekend routes");

        RouteWorkspace withFirst = workspace.AddDocument(new RouteDocument(), "first.gpx");
        RouteWorkspace withSecond = withFirst.AddDocument(new RouteDocument(), "second.gpx");

        withSecond.Documents.Count.ShouldBe(2);
        withSecond.Documents.Select(document => document.Id).Distinct().Count().ShouldBe(2);
        withSecond.ActiveDocument.ShouldBeSameAs(withSecond.Documents[1]);
        withSecond.Documents[0].Id.ShouldBe(withFirst.Documents[0].Id);
    }

    [Fact]
    public void RejectsAnActiveDocumentOutsideTheWorkspace()
    {
        Should.Throw<ArgumentException>(() =>
            new RouteWorkspace(Guid.NewGuid(), "Routes", [], Guid.NewGuid()));
    }

    [Fact]
    public void VisibilitySelectionAndActiveStateAreIndependent()
    {
        RouteWorkspace workspace = new RouteWorkspace(Guid.NewGuid(), "Routes")
            .AddDocument(new RouteDocument(), "first.gpx")
            .AddDocument(new RouteDocument(), "second.gpx");
        Guid firstId = workspace.Documents[0].Id;
        Guid secondId = workspace.Documents[1].Id;

        RouteWorkspace changed = workspace.Select(firstId).SetVisibility(secondId, false);

        changed.ActiveDocumentId.ShouldBe(secondId);
        changed.SelectedDocumentId.ShouldBe(firstId);
        changed.Documents[0].IsVisible.ShouldBeTrue();
        changed.Documents[1].IsVisible.ShouldBeFalse();
    }

    [Fact]
    public void ClosingDocumentPreservesOtherCanonicalDocuments()
    {
        var firstDocument = new RouteDocument(metadata: new RouteMetadata(name: "First"));
        var secondDocument = new RouteDocument(metadata: new RouteMetadata(name: "Second"));
        RouteWorkspace workspace = new RouteWorkspace(Guid.NewGuid(), "Routes")
            .AddDocument(firstDocument, "first.gpx")
            .AddDocument(secondDocument, "second.gpx");

        RouteWorkspace closed = workspace.Close(workspace.Documents[1].Id);

        closed.Documents.Count.ShouldBe(1);
        closed.Documents[0].Document.ShouldBeSameAs(firstDocument);
        closed.ActiveDocumentId.ShouldBe(closed.Documents[0].Id);
    }

    [Fact]
    public void ChildPresentationOverridesInheritWithoutChangingCanonicalDocument()
    {
        var routeDocument = new RouteDocument(tracks: [new Track(segments: [new TrackSegment([])])]);
        RouteWorkspace workspace = new RouteWorkspace(Guid.NewGuid(), "Routes").AddDocument(routeDocument);
        Guid id = workspace.Documents[0].Id;

        RouteWorkspace changed = workspace
            .SetNodeVisibility(id, new WorkspaceNode(WorkspaceNodeKind.Track, 0), false)
            .SetNodeColour(id, new WorkspaceNode(WorkspaceNodeKind.Segment, 0, 0), "#abcdef");

        changed.Documents[0].IsNodeVisible(new WorkspaceNode(WorkspaceNodeKind.Segment, 0, 0)).ShouldBeFalse();
        changed.Documents[0].NodeColour(new WorkspaceNode(WorkspaceNodeKind.Segment, 0, 0)).ShouldBe("#abcdef");
        changed.Documents[0].Document.ShouldBeSameAs(routeDocument);
    }

    [Fact]
    public void ShowingAParentClearsHiddenDescendantOverrides()
    {
        var routeDocument = new RouteDocument(tracks: [new Track(segments: [new TrackSegment([])])]);
        RouteWorkspace workspace = new RouteWorkspace(Guid.NewGuid(), "Routes").AddDocument(routeDocument);
        Guid id = workspace.Documents[0].Id;
        var track = new WorkspaceNode(WorkspaceNodeKind.Track, 0);
        var segment = new WorkspaceNode(WorkspaceNodeKind.Segment, 0, 0);

        RouteWorkspace shown = workspace.SetNodeVisibility(id, segment, false)
            .SetNodeVisibility(id, track, false)
            .SetNodeVisibility(id, track, true);

        shown.Documents[0].IsNodeVisible(segment).ShouldBeTrue();
        shown.Documents[0].PresentationOverrides.ContainsKey(segment).ShouldBeFalse();
    }

    [Fact]
    public void UpdatesEditableInfoWithoutLosingDocumentContent()
    {
        var document = new RouteDocument(
            tracks: [new Track("Old", [new TrackSegment([])], "cycling")],
            metadata: new RouteMetadata("Document", "Old description"),
            unsupportedExtensionXml: ["<x:test xmlns:x='urn:test' />"]);
        RouteWorkspace workspace = new RouteWorkspace(Guid.NewGuid(), "Routes").AddDocument(document);
        Guid id = workspace.Documents[0].Id;

        RouteWorkspace changed = workspace.UpdateNodeInfo(id, null, "Renamed", "New description")
            .UpdateNodeInfo(id, new WorkspaceNode(WorkspaceNodeKind.Track, 0), "New track", null);

        changed.Documents[0].Document.Metadata!.Name.ShouldBe("Renamed");
        changed.Documents[0].Document.Metadata!.Description.ShouldBe("New description");
        changed.Documents[0].Document.Tracks[0].Name.ShouldBe("New track");
        changed.Documents[0].Document.Tracks[0].Type.ShouldBe("cycling");
        changed.Documents[0].Document.UnsupportedExtensionXml.ShouldBe(document.UnsupportedExtensionXml);
    }
}
