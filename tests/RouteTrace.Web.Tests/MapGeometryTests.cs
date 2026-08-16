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
        geometry.Endpoints.Count.ShouldBe(4);
        geometry.Endpoints[0].Coordinate.ShouldBe([12d, 55d]);
        geometry.Endpoints[1].Coordinate.ShouldBe([14d, 57d]);
    }

    [Fact]
    public void IncludesRoutesAndWaypoints()
    {
        var document = new RouteDocument(
            routes: [new Route("Route", [Point(55, 12), Point(56, 13)])],
            waypoints: [new Waypoint(Point(57, 14), "Stop", Symbol: "Park")]);

        MapGeometry geometry = MapGeometry.FromDocument(document);

        geometry.Routes.Single().Count.ShouldBe(2);
        geometry.Waypoints.Single().Coordinate.ShouldBe([14d, 57d]);
        geometry.Waypoints.Single().Name.ShouldBe("Stop");
        geometry.Waypoints.Single().Symbol.ShouldBe("Park");
        geometry.Endpoints.Count.ShouldBe(2);
    }

    [Fact]
    public void UsesWholeTrackEndpointsAndMarksLoops()
    {
        var document = new RouteDocument(tracks: [new Track(segments:
        [
            new TrackSegment([Point(55, 12), Point(56, 13)]),
            new TrackSegment([]),
            new TrackSegment([Point(57, 14), Point(55, 12)])
        ])]);

        MapEndpoint[] endpoints = [.. MapGeometry.FromDocument(document).Endpoints];

        endpoints.Length.ShouldBe(2);
        endpoints.ShouldAllBe(endpoint => endpoint.Overlap);
        endpoints[0].EndpointKind.ShouldBe("start");
        endpoints[1].EndpointKind.ShouldBe("finish");
    }

    private static RoutePoint Point(double latitude, double longitude) =>
        new(new GeoCoordinate(latitude, longitude));
}
