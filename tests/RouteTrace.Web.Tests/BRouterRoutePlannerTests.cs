using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using RouteTrace.Core.Routes.Geometry;
using RouteTrace.Core.Routing;
using RouteTrace.Web.Features.Routing;

namespace RouteTrace.Web.Tests;

public sealed class BRouterRoutePlannerTests
{
    [Fact]
    public async Task SendsOrderedAnchorsAndParsesGeoJsonGeometry()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, """
            {"type":"FeatureCollection","features":[{"type":"Feature","geometry":{"type":"LineString","coordinates":[[12.5,55.6,10],[12.6,55.7,20]]}}]}
            """);
        var planner = CreatePlanner(handler);

        RoutePlanResult result = await planner.PlanAsync(new RoutePlanRequest([
            new GeoCoordinate(55.6, 12.5),
            new GeoCoordinate(55.7, 12.6)
        ]), TestContext.Current.CancellationToken);

        result.Status.ShouldBe(RoutePlanStatus.Success);
        result.Geometry.Select(point => point.Coordinate).ShouldBe([
            new GeoCoordinate(55.6, 12.5),
            new GeoCoordinate(55.7, 12.6)
        ]);
        result.Geometry.Select(point => point.ElevationMetres).ShouldBe([10, 20]);
        handler.RequestUri!.Query.ShouldContain("profile=fastbike");
        Uri.UnescapeDataString(handler.RequestUri.Query).ShouldContain("lonlats=12.5,55.6|12.6,55.7");
    }

    [Theory]
    [InlineData(BicycleRoutingProfile.Cycling, "fastbike")]
    [InlineData(BicycleRoutingProfile.Gravel, "gravel")]
    [InlineData(BicycleRoutingProfile.MountainBike, "mtb")]
    public async Task MapsBicycleModesToBRouterProfiles(BicycleRoutingProfile profile, string expected)
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, """
            {"type":"Feature","geometry":{"type":"LineString","coordinates":[[12.5,55.6],[12.6,55.7]]}}
            """);

        await CreatePlanner(handler).PlanAsync(new RoutePlanRequest([
            new GeoCoordinate(55.6, 12.5),
            new GeoCoordinate(55.7, 12.6)
        ], profile), TestContext.Current.CancellationToken);

        handler.RequestUri!.Query.ShouldContain($"profile={expected}");
    }

    [Fact]
    public async Task DistinguishesNoRouteFromProviderFailure()
    {
        RoutePlanResult noRoute = await CreatePlanner(new RecordingHandler(
            HttpStatusCode.BadRequest,
            "no track found")).PlanAsync(new RoutePlanRequest([
                new GeoCoordinate(55.6, 12.5), new GeoCoordinate(55.7, 12.6)
            ]), TestContext.Current.CancellationToken);
        RoutePlanResult failure = await CreatePlanner(new RecordingHandler(
            HttpStatusCode.ServiceUnavailable,
            "maintenance")).PlanAsync(new RoutePlanRequest([
                new GeoCoordinate(55.6, 12.5), new GeoCoordinate(55.7, 12.6)
            ]), TestContext.Current.CancellationToken);

        noRoute.Status.ShouldBe(RoutePlanStatus.NoRoute);
        failure.Status.ShouldBe(RoutePlanStatus.Failure);
    }

    private static BRouterRoutePlanner CreatePlanner(HttpMessageHandler handler) => new(
        new HttpClient(handler),
        Options.Create(new BRouterOptions()));

    private sealed class RecordingHandler(HttpStatusCode status, string payload) : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            });
        }
    }
}
