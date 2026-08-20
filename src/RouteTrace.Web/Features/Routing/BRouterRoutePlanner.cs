using System.Globalization;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RouteTrace.Core.Routes.Geometry;
using RouteTrace.Core.Routing;

namespace RouteTrace.Web.Features.Routing;

public sealed class BRouterRoutePlanner(HttpClient httpClient, IOptions<BRouterOptions> options) : IRoutePlanner
{
    private readonly BRouterOptions settings = options.Value;

    public async Task<RoutePlanResult> PlanAsync(RoutePlanRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Anchors.Count < 2) return RoutePlanResult.NoRoute("At least two anchors are required.");

        string anchors = string.Join('|', request.Anchors.Select(Format));
        string profile = request.Profile switch
        {
            BicycleRoutingProfile.Gravel => settings.GravelProfile,
            BicycleRoutingProfile.MountainBike => settings.MountainBikeProfile,
            _ => settings.CyclingProfile
        };
        string separator = settings.Endpoint.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        string url = $"{settings.Endpoint}{separator}lonlats={Uri.EscapeDataString(anchors)}&profile={Uri.EscapeDataString(profile)}&alternativeidx=0&format=geojson";

        try
        {
            using HttpResponseMessage response = await httpClient.GetAsync(url, cancellationToken);
            string payload = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
                return LooksLikeNoRoute(response.StatusCode, payload)
                    ? RoutePlanResult.NoRoute(CleanMessage(payload))
                    : RoutePlanResult.Failure($"BRouter returned {(int)response.StatusCode} {response.ReasonPhrase}.");

            return Parse(payload);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or InvalidOperationException)
        {
            return RoutePlanResult.Failure("BRouter is currently unavailable or returned an invalid response.");
        }
    }

    private static RoutePlanResult Parse(string payload)
    {
        using JsonDocument document = JsonDocument.Parse(payload);
        JsonElement root = document.RootElement;
        JsonElement coordinates;
        if (root.TryGetProperty("features", out JsonElement features) && features.ValueKind == JsonValueKind.Array &&
            features.GetArrayLength() > 0 &&
            features[0].TryGetProperty("geometry", out JsonElement geometry) &&
            geometry.TryGetProperty("coordinates", out coordinates))
        {
            // GeoJSON FeatureCollection returned by the public BRouter server.
        }
        else if (root.TryGetProperty("geometry", out geometry) && geometry.TryGetProperty("coordinates", out coordinates))
        {
            // Also accept a single GeoJSON Feature from compatible/self-hosted servers.
        }
        else
        {
            return RoutePlanResult.Failure("BRouter returned no route geometry.");
        }

        var points = new List<RoutePoint>();
        foreach (JsonElement coordinate in coordinates.EnumerateArray())
        {
            if (coordinate.ValueKind != JsonValueKind.Array || coordinate.GetArrayLength() < 2) continue;
            double longitude = coordinate[0].GetDouble();
            double latitude = coordinate[1].GetDouble();
            double? elevation = coordinate.GetArrayLength() > 2 && coordinate[2].ValueKind == JsonValueKind.Number
                ? coordinate[2].GetDouble()
                : null;
            points.Add(new RoutePoint(new GeoCoordinate(latitude, longitude), elevation));
        }
        return points.Count >= 2
            ? RoutePlanResult.Success(points)
            : RoutePlanResult.NoRoute();
    }

    private static string Format(GeoCoordinate coordinate) => string.Create(
        CultureInfo.InvariantCulture,
        $"{coordinate.Longitude:G15},{coordinate.Latitude:G15}");

    private static bool LooksLikeNoRoute(HttpStatusCode status, string payload) =>
        status is HttpStatusCode.BadRequest or HttpStatusCode.NotFound or HttpStatusCode.UnprocessableEntity &&
        (payload.Contains("no route", StringComparison.OrdinalIgnoreCase) ||
         payload.Contains("no track", StringComparison.OrdinalIgnoreCase) ||
         payload.Contains("not found", StringComparison.OrdinalIgnoreCase));

    private static string CleanMessage(string payload)
    {
        string message = payload.Trim();
        return string.IsNullOrWhiteSpace(message) || message.Length > 180
            ? "No bicycle route was found between these anchors."
            : message;
    }
}
