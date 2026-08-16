using RouteTrace.Core.Routes.Documents;
using RouteTrace.Core.Routes.Geometry;

namespace RouteTrace.Core.Tests;

public sealed class RouteDocumentTests
{
    [Fact]
    public void EmptyDocumentHasNoBounds()
    {
        var document = new RouteDocument();

        document.Tracks.ShouldBeEmpty();
        document.Routes.ShouldBeEmpty();
        document.Waypoints.ShouldBeEmpty();
        document.CalculateBounds().ShouldBeNull();
    }

    [Fact]
    public void PreservesMultipleTracksAndSegmentsAsDiscontinuities()
    {
        var firstSegment = new TrackSegment([Point(55, 12)]);
        var secondSegment = new TrackSegment([Point(56, 13)]);
        var document = new RouteDocument(
            tracks:
            [
                new Track("First", [firstSegment, secondSegment]),
                new Track("Second", [new TrackSegment()])
            ]);

        document.Tracks.Count.ShouldBe(2);
        document.Tracks[0].Segments.Count.ShouldBe(2);
        document.Tracks[0].Segments[0].ShouldBeSameAs(firstSegment);
        document.Tracks[0].Segments[1].ShouldBeSameAs(secondSegment);
    }

    [Fact]
    public void BoundsIncludeTracksRoutesAndWaypoints()
    {
        var document = new RouteDocument(
            tracks: [new Track(segments: [new TrackSegment([Point(55, 12), Point(57, 14)])])],
            routes: [new Route(points: [Point(54, 15)])],
            waypoints: [new Waypoint(Point(56, 11))]);

        document.CalculateBounds().ShouldBe(new GeoBounds(54, 11, 57, 15));
    }

    [Theory]
    [InlineData(-90.1, 0)]
    [InlineData(90.1, 0)]
    [InlineData(0, -180.1)]
    [InlineData(0, 180.1)]
    [InlineData(double.NaN, 0)]
    [InlineData(0, double.PositiveInfinity)]
    public void RejectsInvalidCoordinates(double latitude, double longitude)
    {
        Should.Throw<ArgumentOutOfRangeException>(
            () => new GeoCoordinate(latitude, longitude));
    }

    [Fact]
    public void PointCanRepresentOptionalElevationAndTime()
    {
        DateTimeOffset time = DateTimeOffset.Parse("2026-07-30T08:00:00Z");
        var point = new RoutePoint(new GeoCoordinate(55, 12), 42.5, time);

        point.ElevationMetres.ShouldBe(42.5);
        point.Time.ShouldBe(time);
    }

    private static RoutePoint Point(double latitude, double longitude) =>
        new(new GeoCoordinate(latitude, longitude));
}
