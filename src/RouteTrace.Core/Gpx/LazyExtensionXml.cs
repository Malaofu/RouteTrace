using System.Collections;
using System.Xml.Linq;

namespace RouteTrace.Core.Gpx;

internal enum GpxExtensionScope
{
    Document,
    Metadata,
    Waypoint,
    Route,
    RoutePoint,
    Track,
    TrackSegment,
    TrackPoint
}

internal sealed class LazyExtensionXml(byte[] sourceXml) : IReadOnlyList<string>
{
    private static readonly XNamespace Gpx = "http://www.topografix.com/GPX/1/1";
    private readonly Lazy<PreservationIndex> _index = new(() => BuildIndex(sourceXml));

    public int Count => _index.Value.AllExtensions.Count;

    public string this[int itemIndex] => Serialize(_index.Value.AllExtensions[itemIndex]);

    public IReadOnlyDictionary<string, string> RootNamespaceDeclarations => _index.Value.RootNamespaceDeclarations;

    public IReadOnlyList<(XName Name, string Value)> RootAttributes => _index.Value.RootAttributes;

    public IReadOnlyList<XElement> StandardChildrenAt(
        GpxExtensionScope scope,
        int firstIndex = -1,
        int secondIndex = -1,
        int thirdIndex = -1) =>
        _index.Value.StandardChildrenByOwner.GetValueOrDefault(
            new ExtensionOwner(scope, firstIndex, secondIndex, thirdIndex)) ?? [];

    public IReadOnlyList<XElement> At(
        GpxExtensionScope scope,
        int firstIndex = -1,
        int secondIndex = -1,
        int thirdIndex = -1) =>
        _index.Value.ExtensionsByOwner.GetValueOrDefault(
            new ExtensionOwner(scope, firstIndex, secondIndex, thirdIndex)) ?? [];

    public IReadOnlyList<string> StringViewAt(
        GpxExtensionScope scope,
        int firstIndex = -1,
        int secondIndex = -1,
        int thirdIndex = -1) =>
        new ExtensionStringView(this, new(scope, firstIndex, secondIndex, thirdIndex));

    public IEnumerator<string> GetEnumerator()
    {
        foreach (XElement element in _index.Value.AllExtensions) yield return Serialize(element);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private IReadOnlyList<XElement> ElementsAt(ExtensionOwner owner) =>
        _index.Value.ExtensionsByOwner.GetValueOrDefault(owner) ?? [];

    private static PreservationIndex BuildIndex(byte[] sourceXml)
    {
        using var input = new MemoryStream(sourceXml, writable: false);
        XDocument document = XDocument.Load(input, LoadOptions.None);
        XElement root = document.Root!;
        var extensionsByOwner = new Dictionary<ExtensionOwner, IReadOnlyList<XElement>>();
        var standardChildrenByOwner = new Dictionary<ExtensionOwner, IReadOnlyList<XElement>>();

        AddExtensions(extensionsByOwner, new(GpxExtensionScope.Document), root);
        XElement? metadata = root.Element(Gpx + "metadata");
        if (metadata is not null)
        {
            AddExtensions(extensionsByOwner, new(GpxExtensionScope.Metadata), metadata);
            AddStandardChildren(standardChildrenByOwner, new(GpxExtensionScope.Metadata), metadata);
        }

        foreach ((XElement waypoint, int waypointIndex) in Indexed(root.Elements(Gpx + "wpt")))
        {
            var owner = new ExtensionOwner(GpxExtensionScope.Waypoint, waypointIndex);
            AddExtensions(extensionsByOwner, owner, waypoint);
            AddStandardChildren(standardChildrenByOwner, owner, waypoint);
        }

        foreach ((XElement route, int routeIndex) in Indexed(root.Elements(Gpx + "rte")))
        {
            var owner = new ExtensionOwner(GpxExtensionScope.Route, routeIndex);
            AddExtensions(extensionsByOwner, owner, route);
            AddStandardChildren(standardChildrenByOwner, owner, route);
            foreach ((XElement point, int pointIndex) in Indexed(route.Elements(Gpx + "rtept")))
            {
                owner = new(GpxExtensionScope.RoutePoint, routeIndex, pointIndex);
                AddExtensions(extensionsByOwner, owner, point);
                AddStandardChildren(standardChildrenByOwner, owner, point);
            }
        }

        foreach ((XElement track, int trackIndex) in Indexed(root.Elements(Gpx + "trk")))
        {
            var owner = new ExtensionOwner(GpxExtensionScope.Track, trackIndex);
            AddExtensions(extensionsByOwner, owner, track);
            AddStandardChildren(standardChildrenByOwner, owner, track);
            foreach ((XElement segment, int segmentIndex) in Indexed(track.Elements(Gpx + "trkseg")))
            {
                owner = new(GpxExtensionScope.TrackSegment, trackIndex, segmentIndex);
                AddExtensions(extensionsByOwner, owner, segment);
                foreach ((XElement point, int pointIndex) in Indexed(segment.Elements(Gpx + "trkpt")))
                {
                    owner = new(GpxExtensionScope.TrackPoint, trackIndex, segmentIndex, pointIndex);
                    AddExtensions(extensionsByOwner, owner, point);
                    AddStandardChildren(standardChildrenByOwner, owner, point);
                }
            }
        }

        IReadOnlyDictionary<string, string> rootNamespaceDeclarations = root.Attributes()
            .Where(attribute => attribute.IsNamespaceDeclaration && attribute.Name.LocalName != "xmlns")
            .ToDictionary(attribute => attribute.Name.LocalName, attribute => attribute.Value, StringComparer.Ordinal);
        IReadOnlyList<(XName Name, string Value)> rootAttributes = root.Attributes()
            .Where(attribute => !attribute.IsNamespaceDeclaration
                && attribute.Name.LocalName is not "creator" and not "version")
            .Select(attribute => (attribute.Name, attribute.Value))
            .ToArray();
        XElement[] allExtensions = document.Descendants(Gpx + "extensions")
            .Elements()
            .Where(element => element.Name.Namespace != Gpx)
            .ToArray();
        return new(
            allExtensions, extensionsByOwner, standardChildrenByOwner,
            rootNamespaceDeclarations, rootAttributes);
    }

    private static IEnumerable<(XElement Element, int Index)> Indexed(IEnumerable<XElement> elements) =>
        elements.Select((element, index) => (element, index));

    private static void AddExtensions(
        IDictionary<ExtensionOwner, IReadOnlyList<XElement>> index,
        ExtensionOwner owner,
        XElement parent)
    {
        XElement[] values = parent.Elements(Gpx + "extensions")
            .Elements()
            .Where(element => element.Name.Namespace != Gpx)
            .ToArray();
        if (values.Length > 0) index.Add(owner, values);
    }

    private static void AddStandardChildren(
        IDictionary<ExtensionOwner, IReadOnlyList<XElement>> index,
        ExtensionOwner owner,
        XElement parent)
    {
        XElement[] values = parent.Elements()
            .Where(element => element.Name.Namespace == Gpx
                && element.Name.LocalName is not "extensions"
                    and not "metadata" and not "wpt" and not "rte" and not "rtept"
                    and not "trk" and not "trkseg" and not "trkpt")
            .ToArray();
        if (values.Length > 0) index.Add(owner, values);
    }

    private static string Serialize(XElement element) => element.ToString(SaveOptions.DisableFormatting);

    private sealed class ExtensionStringView(LazyExtensionXml source, ExtensionOwner owner) : IReadOnlyList<string>
    {
        public int Count => source.ElementsAt(owner).Count;

        public string this[int index] => Serialize(source.ElementsAt(owner)[index]);

        public IEnumerator<string> GetEnumerator()
        {
            foreach (XElement element in source.ElementsAt(owner)) yield return Serialize(element);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed record PreservationIndex(
        IReadOnlyList<XElement> AllExtensions,
        IReadOnlyDictionary<ExtensionOwner, IReadOnlyList<XElement>> ExtensionsByOwner,
        IReadOnlyDictionary<ExtensionOwner, IReadOnlyList<XElement>> StandardChildrenByOwner,
        IReadOnlyDictionary<string, string> RootNamespaceDeclarations,
        IReadOnlyList<(XName Name, string Value)> RootAttributes);

    private sealed record ExtensionOwner(GpxExtensionScope Scope, int First = -1, int Second = -1, int Third = -1);
}
