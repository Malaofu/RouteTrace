using RouteTrace.Core.Routes.Geometry;

namespace RouteTrace.Core.Routes.Documents;

public sealed class Route
{
    public Route(
        string? name = null,
        IEnumerable<RoutePoint>? points = null,
        IEnumerable<string>? unsupportedExtensionXml = null)
    {
        Name = name;
        Points = RouteDocument.Snapshot(points, nameof(points));
        UnsupportedExtensionXml = unsupportedExtensionXml is null
            ? []
            : Array.AsReadOnly([.. unsupportedExtensionXml]);
    }

    internal Route(
        string? name,
        IReadOnlyList<RoutePoint> points,
        IReadOnlyList<string> unsupportedExtensionXml)
    {
        Name = name;
        Points = points;
        UnsupportedExtensionXml = unsupportedExtensionXml;
    }

    public string? Name { get; }
    public IReadOnlyList<RoutePoint> Points { get; }
    public IReadOnlyList<string> UnsupportedExtensionXml { get; }
}
