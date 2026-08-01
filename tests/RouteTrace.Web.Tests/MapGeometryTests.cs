using RouteTrace.Core.Routes;
using RouteTrace.Web.Features.Map;

namespace RouteTrace.Web.Tests;

public sealed class MapGeometryTests
{
    [Fact]
    public void KeepsSegmentsSeparateAndUsesLongitudeLatitudeOrder()
    {
        var document = new RouteDocument(tracks:
        [
            new Track("First",
            [
                new TrackSegment([Point(55, 12), Point(56, 13)]),
                new TrackSegment([Point(57, 14)])
            ]),
            new Track("Second", [new TrackSegment([Point(58, 15)])])
        ]);

        MapGeometry geometry = MapGeometry.FromDocument(document);

        geometry.Tracks.Count.ShouldBe(2);
        geometry.Tracks[0].Segments.Count.ShouldBe(2);
        geometry.Tracks[0].Segments[0][0].ShouldBe([12d, 55d]);
        geometry.Tracks[0].Segments[1][0].ShouldBe([14d, 57d]);
    }

    [Fact]
    public void IncludesRoutesAndWaypoints()
    {
        var document = new RouteDocument(
            routes: [new Route("Route", [Point(55, 12), Point(56, 13)])],
            waypoints: [new Waypoint(Point(57, 14), "Stop")]);

        MapGeometry geometry = MapGeometry.FromDocument(document);

        geometry.Routes.Single().Count.ShouldBe(2);
        geometry.Waypoints.Single().ShouldBe([14d, 57d]);
    }

    private static RoutePoint Point(double latitude, double longitude) =>
        new(new GeoCoordinate(latitude, longitude));
}
