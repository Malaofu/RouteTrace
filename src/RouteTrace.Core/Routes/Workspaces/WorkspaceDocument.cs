using RouteTrace.Core.Routes.Documents;

namespace RouteTrace.Core.Routes.Workspaces;

public sealed class WorkspaceDocument
{
    private static readonly string[] DefaultColours = ["#2563eb", "#dc2626", "#16a34a", "#9333ea", "#ea580c", "#0891b2"];

    public WorkspaceDocument(Guid id, RouteDocument document, string? sourceFileName = null, bool isVisible = true, string colour = "#2563eb", IEnumerable<NodePresentationOverride>? presentationOverrides = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("A document ID is required.", nameof(id));
        Id = id;
        Document = document ?? throw new ArgumentNullException(nameof(document));
        SourceFileName = string.IsNullOrWhiteSpace(sourceFileName) ? null : sourceFileName;
        IsVisible = isVisible;
        Colour = string.IsNullOrWhiteSpace(colour) ? "#2563eb" : colour;
        PresentationOverrides = (presentationOverrides ?? []).ToDictionary(item => item.Node);
    }

    public Guid Id { get; }
    public RouteDocument Document { get; }
    public string? SourceFileName { get; }
    public bool IsVisible { get; }
    public string Colour { get; }
    public IReadOnlyDictionary<WorkspaceNode, NodePresentationOverride> PresentationOverrides { get; }

    public static string DefaultColour(int index) => DefaultColours[index % DefaultColours.Length];

    public WorkspaceDocument WithVisibility(bool visible) => new(Id, Document, SourceFileName, visible, Colour,
        visible ? PresentationOverrides.Values.Select(item => item with { IsVisible = null }).Where(HasValue) : PresentationOverrides.Values);

    public WorkspaceDocument WithColour(string colour) =>
        new(Id, Document, SourceFileName, IsVisible, colour, PresentationOverrides.Values);

    public WorkspaceDocument WithDocument(RouteDocument document) =>
        new(Id, document, SourceFileName, IsVisible, Colour, PresentationOverrides.Values);

    public WorkspaceDocument AddTrack(string? name = null)
    {
        string trackName = name ?? $"Route {Document.Tracks.Count + 1}";
        return WithDocument(CopyDocument(tracks: Document.Tracks.Append(new Track(trackName))));
    }

    public WorkspaceDocument AddSegment(int trackIndex)
    {
        Track[] tracks = [.. Document.Tracks];
        Track track = tracks[trackIndex];
        tracks[trackIndex] = new Track(track.Name, track.Segments.Append(new TrackSegment()), track.Type);
        return WithDocument(CopyDocument(tracks: tracks));
    }

    public WorkspaceDocument DeleteNode(WorkspaceNode node)
    {
        RouteDocument changed = node.Kind switch
        {
            WorkspaceNodeKind.Track => CopyDocument(tracks: Document.Tracks.Where((_, index) => index != node.PrimaryIndex)),
            WorkspaceNodeKind.Segment => DeleteSegment(node.PrimaryIndex, node.SecondaryIndex),
            WorkspaceNodeKind.Route => CopyDocument(routes: Document.Routes.Where((_, index) => index != node.PrimaryIndex)),
            _ => throw new ArgumentException("Only routes and segments can be deleted.", nameof(node))
        };
        return new WorkspaceDocument(Id, changed, SourceFileName, IsVisible, Colour);
    }

    public WorkspaceDocument WithNodeVisibility(WorkspaceNode node, bool? visible)
    {
        var overrides = PresentationOverrides.Values
            .Where(item => visible is not true || !IsDescendantOrSelf(item.Node, node))
            .ToDictionary(item => item.Node);
        string? colour = overrides.GetValueOrDefault(node)?.Colour ?? PresentationOverrides.GetValueOrDefault(node)?.Colour;
        if (visible is not null || colour is not null) overrides[node] = new(node, visible, colour);
        return new WorkspaceDocument(Id, Document, SourceFileName, IsVisible, Colour, overrides.Values.Where(HasValue));
    }

    public WorkspaceDocument WithNodeColour(WorkspaceNode node, string? colour) =>
        WithOverride(node, PresentationOverrides.GetValueOrDefault(node)?.IsVisible, colour);

    public bool IsNodeVisible(WorkspaceNode node) => IsVisible &&
        InheritanceChain(node).Select(item => PresentationOverrides.GetValueOrDefault(item)?.IsVisible)
            .LastOrDefault(value => value is not null) is not false;

    public string NodeColour(WorkspaceNode node) =>
        InheritanceChain(node).Select(item => PresentationOverrides.GetValueOrDefault(item)?.Colour)
            .LastOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? Colour;

    public WorkspaceDocument WithNodeInfo(WorkspaceNode? node, string? name, string? description)
    {
        RouteDocument? changed = node switch
        {
            null => WithDocumentInfo(name, description),
            { Kind: WorkspaceNodeKind.Track } value => WithTrackInfo(value.PrimaryIndex, name),
            { Kind: WorkspaceNodeKind.Route } value => WithRouteInfo(value.PrimaryIndex, name),
            { Kind: WorkspaceNodeKind.Waypoint } value => WithWaypointInfo(value.PrimaryIndex, name, description),
            _ => null
        };
        return changed is null
            ? this
            : new WorkspaceDocument(Id, changed, SourceFileName, IsVisible, Colour, PresentationOverrides.Values);
    }

    private WorkspaceDocument WithOverride(WorkspaceNode node, bool? visible, string? colour)
    {
        var overrides = PresentationOverrides.ToDictionary(pair => pair.Key, pair => pair.Value);
        if (visible is null && string.IsNullOrWhiteSpace(colour)) overrides.Remove(node);
        else overrides[node] = new NodePresentationOverride(node, visible, string.IsNullOrWhiteSpace(colour) ? null : colour);
        return new WorkspaceDocument(Id, Document, SourceFileName, IsVisible, Colour, overrides.Values);
    }

    private RouteDocument WithDocumentInfo(string? name, string? description)
    {
        RouteMetadata existing = Document.Metadata ?? new RouteMetadata();
        return CopyDocument(metadata: new RouteMetadata(
            name, description, existing.Time, existing.Links, existing.Author, existing.UnsupportedExtensionXml));
    }

    private RouteDocument WithTrackInfo(int index, string? name)
    {
        Track[] tracks = [.. Document.Tracks];
        Track existing = tracks[index];
        tracks[index] = new Track(name, existing.Segments, existing.Type);
        return CopyDocument(tracks: tracks);
    }

    private RouteDocument WithRouteInfo(int index, string? name)
    {
        Route[] routes = [.. Document.Routes];
        Route existing = routes[index];
        routes[index] = new Route(name, existing.Points, existing.UnsupportedExtensionXml, existing.AnchorIndices);
        return CopyDocument(routes: routes);
    }

    private RouteDocument WithWaypointInfo(int index, string? name, string? description)
    {
        Waypoint[] waypoints = [.. Document.Waypoints];
        waypoints[index] = waypoints[index] with { Name = name, Description = description };
        return CopyDocument(waypoints: waypoints);
    }

    private RouteDocument DeleteSegment(int trackIndex, int segmentIndex)
    {
        Track[] tracks = [.. Document.Tracks];
        Track track = tracks[trackIndex];
        tracks[trackIndex] = new Track(
            track.Name,
            track.Segments.Where((_, index) => index != segmentIndex),
            track.Type);
        return CopyDocument(tracks: tracks);
    }

    private RouteDocument CopyDocument(IEnumerable<Track>? tracks = null, IEnumerable<Route>? routes = null, IEnumerable<Waypoint>? waypoints = null, RouteMetadata? metadata = null) =>
        new(tracks ?? Document.Tracks, routes ?? Document.Routes, waypoints ?? Document.Waypoints,
            metadata ?? Document.Metadata, Document.UnsupportedExtensionXml, Document.UnsupportedExtensionNamespaces);

    private static bool HasValue(NodePresentationOverride item) => item.IsVisible is not null || item.Colour is not null;

    private static bool IsDescendantOrSelf(WorkspaceNode candidate, WorkspaceNode ancestor) => candidate == ancestor ||
        (ancestor.Kind == WorkspaceNodeKind.Track && candidate.Kind == WorkspaceNodeKind.Segment && candidate.PrimaryIndex == ancestor.PrimaryIndex) ||
        (ancestor.Kind == WorkspaceNodeKind.WaypointGroup && candidate.Kind == WorkspaceNodeKind.Waypoint);

    private static IEnumerable<WorkspaceNode> InheritanceChain(WorkspaceNode node)
    {
        if (node.Kind == WorkspaceNodeKind.Segment) yield return new(WorkspaceNodeKind.Track, node.PrimaryIndex);
        if (node.Kind == WorkspaceNodeKind.Waypoint) yield return new(WorkspaceNodeKind.WaypointGroup);
        yield return node;
    }
}
