using RouteTrace.Core.Routes.Documents;
using RouteTrace.Core.Routes.Geometry;
using RouteTrace.Core.Routes.Workspaces;
using RouteTrace.Web.Features.Workspaces;

namespace RouteTrace.Web.Tests;

public sealed class DocumentTreeTests
{
    [Fact]
    public void ExpandsNewDocumentsTracksAndWaypointGroups()
    {
        RouteWorkspace workspace = new RouteWorkspace(Guid.NewGuid(), "Routes")
            .AddDocument(new RouteDocument(
                tracks: [new Track(segments: [new TrackSegment([])])],
                waypoints: [new Waypoint(new RoutePoint(new GeoCoordinate(55, 12)))]));
        WorkspaceDocument document = workspace.Documents.Single();
        var state = new DocumentTreeExpansionState();

        state.ExpandNewDocuments(workspace);

        state.IsExpanded(DocumentTreeExpansionState.DocumentKey(document.Id)).ShouldBeTrue();
        state.IsExpanded(DocumentTreeExpansionState.TrackKey(document.Id, 0)).ShouldBeTrue();
        state.IsExpanded(DocumentTreeExpansionState.WaypointGroupKey(document.Id)).ShouldBeTrue();
    }

    [Fact]
    public void PreservesExistingExpansionChoicesWhenDocumentsAreAdded()
    {
        RouteWorkspace workspace = new RouteWorkspace(Guid.NewGuid(), "Routes")
            .AddDocument(new RouteDocument());
        WorkspaceDocument first = workspace.Documents.Single();
        string firstKey = DocumentTreeExpansionState.DocumentKey(first.Id);
        var state = new DocumentTreeExpansionState();
        state.ExpandNewDocuments(workspace);
        state.Toggle(firstKey);

        RouteWorkspace updated = workspace.AddDocument(new RouteDocument());
        WorkspaceDocument second = updated.Documents[1];
        state.ExpandNewDocuments(updated);

        state.IsExpanded(firstKey).ShouldBeFalse();
        state.IsExpanded(DocumentTreeExpansionState.DocumentKey(second.Id)).ShouldBeTrue();
    }

    [Fact]
    public void BuildsCanonicalSelectionForEverySemanticNodeKind()
    {
        var routeDocument = new RouteDocument(
            tracks: [new Track("Track", [new TrackSegment([])])],
            routes: [new Route("Route", [])],
            waypoints: [new Waypoint(new RoutePoint(new GeoCoordinate(55, 12)), "Waypoint")]);
        var document = new WorkspaceDocument(Guid.NewGuid(), routeDocument, "route.gpx");

        DocumentTreeTargetFactory.Create(document, null).Selection.WholeDocument.ShouldBeTrue();
        DocumentTreeTargetFactory.Create(document, new(WorkspaceNodeKind.Track, 0)).Selection.TrackIndex.ShouldBe(0);
        DocumentTreeTargetFactory.Create(document, new(WorkspaceNodeKind.Segment, 0, 0)).Selection.SegmentIndex.ShouldBe(0);
        DocumentTreeTargetFactory.Create(document, new(WorkspaceNodeKind.Route, 0)).Selection.RouteIndex.ShouldBe(0);
        DocumentTreeTargetFactory.Create(document, new(WorkspaceNodeKind.WaypointGroup)).Selection.WaypointGroup.ShouldBeTrue();
        DocumentTreeTargetFactory.Create(document, new(WorkspaceNodeKind.Waypoint, 0)).Selection.WaypointIndex.ShouldBe(0);
    }
}
