using RouteTrace.Core.Routes.Geometry;
using RouteTrace.Core.Editing;

namespace RouteTrace.Core.Routes.Documents;

public sealed class Route
{
    public Route(
        string? name = null,
        IEnumerable<RoutePoint>? points = null,
        IEnumerable<string>? unsupportedExtensionXml = null,
        IEnumerable<int>? anchorIndices = null)
    {
        Name = name;
        Points = RouteDocument.Snapshot(points, nameof(points));
        UnsupportedExtensionXml = unsupportedExtensionXml is null
            ? []
            : Array.AsReadOnly([.. unsupportedExtensionXml]);
        AnchorIndices = RouteAnchorSelector.ValidateOrSelect(Points, anchorIndices);
    }

    internal Route(
        string? name,
        IReadOnlyList<RoutePoint> points,
        IReadOnlyList<string> unsupportedExtensionXml)
    {
        Name = name;
        Points = points;
        UnsupportedExtensionXml = unsupportedExtensionXml;
        AnchorIndices = RouteAnchorSelector.Select(points);
    }

    public string? Name { get; }
    public IReadOnlyList<RoutePoint> Points { get; }
    public IReadOnlyList<string> UnsupportedExtensionXml { get; }
    public IReadOnlyList<int> AnchorIndices { get; }
}
