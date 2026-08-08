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
}
