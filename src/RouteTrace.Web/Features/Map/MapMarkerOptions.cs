namespace RouteTrace.Web.Features.Map;

public sealed class MapMarkerOptions
{
    public const string SectionName = "MapMarkers";
    public const double PinIntrinsicHeightPixels = 50;

    public string DefaultIcon { get; set; } = "generic";
    public double PinScale { get; set; } = 0.67;
    public double SelectedPinScale { get; set; } = 0.96;
    public MapMarkerAssets Assets { get; set; } = new();
    public Dictionary<string, string> Symbols { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class MapMarkerAssets
{
    public string PinFill { get; set; } = "pin-fill";
    public string PinOutline { get; set; } = "pin-outline";
    public string Finish { get; set; } = "finish";
}
