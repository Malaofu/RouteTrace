using RouteTrace.Core.Routes;

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
}
