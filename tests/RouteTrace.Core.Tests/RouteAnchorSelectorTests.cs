using RouteTrace.Core.Editing;
using RouteTrace.Core.Routes.Documents;
using RouteTrace.Core.Routes.Geometry;

namespace RouteTrace.Core.Tests;

public sealed class RouteAnchorSelectorTests
{
    [Fact]
    public void ElevatesEndpointsAndMeaningfulDirectionChanges()
    {
        RoutePoint[] points =
        [
            Point(55, 12),
            Point(55, 12.001),
            Point(55.001, 12.001),
            Point(55.001, 12.002)
        ];

        IReadOnlyList<int> anchors = RouteAnchorSelector.Select(points);

        anchors[0].ShouldBe(0);
        anchors[^1].ShouldBe(points.Length - 1);
        anchors.ShouldContain(1);
        anchors.ShouldContain(2);
    }

    [Fact]
    public void ExplicitAnchorsKeepIntermediateGeometryNonInteractive()
    {
        RoutePoint[] points = [Point(55, 12), Point(55.0005, 12.0005), Point(55.001, 12.001)];

        var editor = new ManualRouteEditor(points, [0, 2]);

        editor.RoutePoints.Count.ShouldBe(3);
        editor.AnchorPoints.Select(point => point.Coordinate).ShouldBe([points[0].Coordinate, points[2].Coordinate]);
        editor.Legs.Single().Count.ShouldBe(3);
    }

    [Fact]
    public void RejectsAnchorsThatDoNotIncludeGeometryEndpoints()
    {
        RoutePoint[] points = [Point(55, 12), Point(55.001, 12.001), Point(55.002, 12.002)];

        Should.Throw<ArgumentException>(() => new TrackSegment(points, [1, 2]));
    }

    private static RoutePoint Point(double latitude, double longitude) =>
        new(new GeoCoordinate(latitude, longitude));
}
