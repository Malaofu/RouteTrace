namespace RouteTrace.Web.Features.Routing;

public sealed class BRouterOptions
{
    public const string SectionName = "Routing:BRouter";
    public string Endpoint { get; set; } = "https://brouter.de/brouter";
    public string CyclingProfile { get; set; } = "fastbike";
    public string GravelProfile { get; set; } = "gravel";
    public string MountainBikeProfile { get; set; } = "mtb";
}
