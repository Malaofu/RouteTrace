using System.Collections.ObjectModel;

namespace RouteTrace.Core.Routes;

public sealed class RouteDocument
{
    public RouteDocument(
        IEnumerable<Track>? tracks = null,
        IEnumerable<Route>? routes = null,
        IEnumerable<Waypoint>? waypoints = null,
        RouteMetadata? metadata = null,
        IEnumerable<string>? unsupportedExtensionXml = null,
        IEnumerable<string>? unsupportedExtensionNamespaces = null)
    {
        Tracks = Snapshot(tracks, nameof(tracks));
        Routes = Snapshot(routes, nameof(routes));
        Waypoints = Snapshot(waypoints, nameof(waypoints));
        Metadata = metadata;
        UnsupportedExtensionXml = SnapshotValues(unsupportedExtensionXml, nameof(unsupportedExtensionXml));
        UnsupportedExtensionNamespaces = unsupportedExtensionNamespaces is null
            ? ExtensionNamespaces(UnsupportedExtensionXml)
            : SnapshotValues(unsupportedExtensionNamespaces, nameof(unsupportedExtensionNamespaces));
    }

    public IReadOnlyList<Track> Tracks { get; }

    public IReadOnlyList<Route> Routes { get; }

    public IReadOnlyList<Waypoint> Waypoints { get; }

    public RouteMetadata? Metadata { get; }

    public IReadOnlyList<string> UnsupportedExtensionXml { get; }

    public IReadOnlyList<string> UnsupportedExtensionNamespaces { get; }

    internal RouteDocument(
        IEnumerable<Track> tracks,
        IEnumerable<Route> routes,
        IEnumerable<Waypoint> waypoints,
        RouteMetadata? metadata,
        IReadOnlyList<string> unsupportedExtensionXml,
        IReadOnlyList<string> unsupportedExtensionNamespaces)
    {
        Tracks = Snapshot(tracks, nameof(tracks));
        Routes = Snapshot(routes, nameof(routes));
        Waypoints = Snapshot(waypoints, nameof(waypoints));
        Metadata = metadata;
        UnsupportedExtensionXml = unsupportedExtensionXml;
        UnsupportedExtensionNamespaces = unsupportedExtensionNamespaces;
    }

    public GeoBounds? CalculateBounds()
    {
        IEnumerable<GeoCoordinate> coordinates =
            Tracks.SelectMany(track => track.Segments)
                .SelectMany(segment => segment.Points)
                .Select(point => point.Coordinate)
                .Concat(
                    Routes.SelectMany(route => route.Points)
                        .Select(point => point.Coordinate))
                .Concat(Waypoints.Select(waypoint => waypoint.Point.Coordinate));

        using IEnumerator<GeoCoordinate> enumerator = coordinates.GetEnumerator();
        if (!enumerator.MoveNext())
        {
            return null;
        }

        double south = enumerator.Current.Latitude;
        double west = enumerator.Current.Longitude;
        double north = south;
        double east = west;

        while (enumerator.MoveNext())
        {
            south = Math.Min(south, enumerator.Current.Latitude);
            west = Math.Min(west, enumerator.Current.Longitude);
            north = Math.Max(north, enumerator.Current.Latitude);
            east = Math.Max(east, enumerator.Current.Longitude);
        }

        return new GeoBounds(south, west, north, east);
    }

    internal static IReadOnlyList<T> Snapshot<T>(
        IEnumerable<T>? items,
        string parameterName)
        where T : class
    {
        if (items is null)
        {
            return [];
        }

        T[] snapshot = [.. items];
        if (snapshot.Any(item => item is null))
        {
            throw new ArgumentException("Collections cannot contain null items.", parameterName);
        }

        return new ReadOnlyCollection<T>(snapshot);
    }

    private static IReadOnlyList<T> SnapshotValues<T>(IEnumerable<T>? items, string parameterName)
    {
        if (items is null)
        {
            return [];
        }

        T[] snapshot = [.. items];
        if (snapshot.Any(item => item is null))
        {
            throw new ArgumentException("Collections cannot contain null items.", parameterName);
        }

        return Array.AsReadOnly(snapshot);
    }

    private static IReadOnlyList<string> ExtensionNamespaces(IEnumerable<string> extensionXml) =>
        extensionXml.Select(xml => System.Xml.Linq.XDocument.Parse(xml).Root?.Name.NamespaceName)
            .Where(namespaceName => !string.IsNullOrWhiteSpace(namespaceName))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .Cast<string>()
            .ToArray();
}

public sealed class RouteMetadata
{
    public RouteMetadata(
        string? name = null,
        string? description = null,
        DateTimeOffset? time = null,
        IEnumerable<RouteLink>? links = null,
        RouteAuthor? author = null,
        IEnumerable<string>? unsupportedExtensionXml = null)
    {
        Name = name;
        Description = description;
        Time = time;
        Links = links is null ? [] : Array.AsReadOnly([.. links]);
        Author = author;
        UnsupportedExtensionXml = unsupportedExtensionXml is null
            ? []
            : Array.AsReadOnly([.. unsupportedExtensionXml]);
    }

    internal RouteMetadata(
        string? name,
        string? description,
        DateTimeOffset? time,
        IReadOnlyList<RouteLink> links,
        RouteAuthor? author,
        IReadOnlyList<string> unsupportedExtensionXml)
    {
        Name = name;
        Description = description;
        Time = time;
        Links = links;
        Author = author;
        UnsupportedExtensionXml = unsupportedExtensionXml;
    }

    public string? Name { get; }

    public string? Description { get; }

    public DateTimeOffset? Time { get; }

    public IReadOnlyList<RouteLink> Links { get; }

    public RouteAuthor? Author { get; }

    public IReadOnlyList<string> UnsupportedExtensionXml { get; }
}

public sealed record RouteLink(string Href, string? Text = null, string? MimeType = null);

public sealed record RouteAuthor(string? Name = null, string? EmailId = null, string? EmailDomain = null, RouteLink? Link = null);

public sealed class Track
{
    public Track(string? name = null, IEnumerable<TrackSegment>? segments = null, string? type = null)
    {
        Name = name;
        Segments = RouteDocument.Snapshot(segments, nameof(segments));
        Type = type;
    }

    public string? Name { get; }

    /// <summary>
    /// Ordered continuous sections. A boundary between two segments represents
    /// an explicit discontinuity in the track.
    /// </summary>
    public IReadOnlyList<TrackSegment> Segments { get; }

    public string? Type { get; }
}

public sealed class TrackSegment
{
    public TrackSegment(IEnumerable<RoutePoint>? points = null)
    {
        Points = RouteDocument.Snapshot(points, nameof(points));
    }

    public IReadOnlyList<RoutePoint> Points { get; }
}

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

public sealed record Waypoint(
    RoutePoint Point,
    string? Name = null,
    string? Comment = null,
    string? Description = null,
    string? Symbol = null,
    IReadOnlyList<RouteLink>? Links = null)
{
    public RoutePoint Point { get; } =
        Point ?? throw new ArgumentNullException(nameof(Point));

    public IReadOnlyList<RouteLink> Links { get; } = Links ?? [];
}
