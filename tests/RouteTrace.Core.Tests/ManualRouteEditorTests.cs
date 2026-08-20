using RouteTrace.Core.Editing;
using RouteTrace.Core.Routes.Documents;
using RouteTrace.Core.Routes.Geometry;
using GpxRoute = RouteTrace.Core.Routes.Documents.Route;

namespace RouteTrace.Core.Tests;

public sealed class ManualRouteEditorTests
{
    private static readonly GeoCoordinate First = new(56.1, 10.1);
    private static readonly GeoCoordinate Second = new(56.2, 10.2);
    private static readonly GeoCoordinate Third = new(56.3, 10.3);
    private static readonly GeoCoordinate Inserted = new(56.15, 10.15);

    [Fact]
    public void AddsInsertsMovesAndDeletesOrderedPoints()
    {
        var editor = new ManualRouteEditor();
        editor.Add(First);
        editor.Add(Second);
        editor.InsertAfter(0, Inserted);
        editor.Move(1, Third);
        editor.Delete(0);

        editor.Points.ShouldBe([Third, Second]);
    }

    [Fact]
    public void ReversesDirectionAndRotatesAClosedLoopStart()
    {
        var editor = CreateTriangle();
        editor.CloseLoop();
        editor.SetLoopStart(1);
        editor.Reverse();

        editor.Points.ShouldBe([Second, First, Third, Second]);
        editor.IsLoop.ShouldBeTrue();
    }

    [Fact]
    public void UndoAndRedoCoverEditsAndNewEditsClearRedo()
    {
        var editor = CreateTriangle();
        editor.Clear();

        editor.Undo().ShouldBeTrue();
        editor.Points.ShouldBe([First, Second, Third]);
        editor.Redo().ShouldBeTrue();
        editor.Points.ShouldBeEmpty();
        editor.Undo().ShouldBeTrue();
        editor.Delete(1);

        editor.CanRedo.ShouldBeFalse();
        editor.Points.ShouldBe([First, Third]);
    }

    [Fact]
    public void MovingLoopStartKeepsClosingPointTogether()
    {
        var editor = CreateTriangle();
        editor.CloseLoop();

        editor.Move(0, Inserted);

        editor.Points.ShouldBe([Inserted, Second, Third, Inserted]);
    }

    [Fact]
    public void ConvertsPointsToCanonicalTrackGeometry()
    {
        var editor = CreateTriangle();

        var document = editor.ToDocument();

        document.Tracks.Count.ShouldBe(1);
        document.Tracks[0].Segments.Count.ShouldBe(1);
        document.Tracks[0].Segments[0].Points.Select(point => point.Coordinate)
            .ShouldBe([First, Second, Third]);
    }

    [Fact]
    public void ReplacesMapGeometryAsOneEditAndPreservesExistingPointMetadata()
    {
        DateTimeOffset time = DateTimeOffset.Parse("2026-08-18T10:00:00Z");
        var editor = new ManualRouteEditor([
            new RoutePoint(First, 42, time),
            new RoutePoint(Second, 43, time.AddMinutes(1))
        ]);

        editor.ReplaceCoordinates([First, Inserted, Second]).ShouldBeTrue();

        editor.RoutePoints[0].ElevationMetres.ShouldBe(42);
        editor.RoutePoints[0].Time.ShouldBe(time);
        editor.RoutePoints[1].ElevationMetres.ShouldBeNull();
        editor.RoutePoints[2].ElevationMetres.ShouldBe(43);
        editor.RoutePoints[2].Coordinate.ShouldBe(Second);
        editor.Undo().ShouldBeTrue();
        editor.Points.ShouldBe([First, Second]);
    }

    [Fact]
    public void ExistingSegmentAndRouteEditsPreserveTheRestOfTheDocument()
    {
        var untouchedSegment = new TrackSegment([new RoutePoint(Third)]);
        var route = new GpxRoute("Route", [new RoutePoint(First)], ["<x:r xmlns:x='urn:r' />"]);
        var document = new RouteDocument(
            tracks: [new Track("Track", [new TrackSegment([new RoutePoint(First)]), untouchedSegment], "cycling")],
            routes: [route],
            metadata: new RouteMetadata("Document"),
            unsupportedExtensionXml: ["<x:d xmlns:x='urn:d' />"]);

        RouteDocument changedSegment = EditableLineTarget.TrackSegment(0, 0)
            .ReplacePoints(document, [new RoutePoint(Second)]);
        RouteDocument changedRoute = EditableLineTarget.Route(0)
            .ReplacePoints(changedSegment, [new RoutePoint(Third)]);

        changedRoute.Tracks[0].Segments[0].Points[0].Coordinate.ShouldBe(Second);
        changedRoute.Tracks[0].Segments[1].ShouldBeSameAs(untouchedSegment);
        changedRoute.Tracks[0].Type.ShouldBe("cycling");
        changedRoute.Routes[0].Points[0].Coordinate.ShouldBe(Third);
        changedRoute.Routes[0].UnsupportedExtensionXml.ShouldBe(route.UnsupportedExtensionXml);
        changedRoute.Metadata.ShouldBeSameAs(document.Metadata);
        changedRoute.UnsupportedExtensionXml.ShouldBe(document.UnsupportedExtensionXml);
    }

    private static ManualRouteEditor CreateTriangle()
    {
        var editor = new ManualRouteEditor();
        editor.Add(First);
        editor.Add(Second);
        editor.Add(Third);
        return editor;
    }
}
