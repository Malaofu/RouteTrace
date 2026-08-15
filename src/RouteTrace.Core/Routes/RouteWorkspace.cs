namespace RouteTrace.Core.Routes;

public sealed class RouteWorkspace
{
    public RouteWorkspace(
        Guid id,
        string name,
        IEnumerable<WorkspaceDocument>? documents = null,
        Guid? activeDocumentId = null,
        Guid? selectedDocumentId = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("A workspace ID is required.", nameof(id));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("A workspace name is required.", nameof(name));

        WorkspaceDocument[] documentSnapshot = documents?.ToArray() ?? [];
        if (documentSnapshot.Any(document => document is null))
            throw new ArgumentException("Documents cannot contain null items.", nameof(documents));
        if (documentSnapshot.Select(document => document.Id).Distinct().Count() != documentSnapshot.Length)
            throw new ArgumentException("Document IDs must be unique.", nameof(documents));
        if (activeDocumentId is not null && documentSnapshot.All(document => document.Id != activeDocumentId))
            throw new ArgumentException("The active document must belong to the workspace.", nameof(activeDocumentId));
        if (selectedDocumentId is not null && documentSnapshot.All(document => document.Id != selectedDocumentId))
            throw new ArgumentException("The selected document must belong to the workspace.", nameof(selectedDocumentId));

        Id = id;
        Name = name.Trim();
        Documents = Array.AsReadOnly(documentSnapshot);
        ActiveDocumentId = activeDocumentId;
        SelectedDocumentId = selectedDocumentId;
    }

    public Guid Id { get; }

    public string Name { get; }

    public IReadOnlyList<WorkspaceDocument> Documents { get; }

    public Guid? ActiveDocumentId { get; }
    public Guid? SelectedDocumentId { get; }

    public WorkspaceDocument? ActiveDocument =>
        ActiveDocumentId is null ? null : Documents.Single(document => document.Id == ActiveDocumentId);

    public RouteWorkspace AddDocument(RouteDocument document, string? sourceFileName = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        string[] colours = ["#2563eb", "#dc2626", "#16a34a", "#9333ea", "#ea580c", "#0891b2"];
        var workspaceDocument = new WorkspaceDocument(Guid.NewGuid(), document, sourceFileName, true, colours[Documents.Count % colours.Length]);
        return new RouteWorkspace(Id, Name, Documents.Append(workspaceDocument), workspaceDocument.Id, workspaceDocument.Id);
    }

    public RouteWorkspace Rename(string name) => new(Id, name, Documents, ActiveDocumentId, SelectedDocumentId);

    public RouteWorkspace Activate(Guid documentId) => new(Id, Name, Documents, documentId, SelectedDocumentId);

    public RouteWorkspace Select(Guid? documentId) => new(Id, Name, Documents, ActiveDocumentId, documentId);

    public RouteWorkspace SetVisibility(Guid documentId, bool visible) => new(
        Id, Name, Documents.Select(document => document.Id == documentId ? document.WithVisibility(visible) : document),
        ActiveDocumentId, SelectedDocumentId);

    public RouteWorkspace SetColour(Guid documentId, string colour) => Replace(documentId, document => document.WithColour(colour));
    public RouteWorkspace SetNodeVisibility(Guid documentId, WorkspaceNode node, bool? visible) => Replace(documentId, document => document.WithNodeVisibility(node, visible));
    public RouteWorkspace SetNodeColour(Guid documentId, WorkspaceNode node, string? colour) => Replace(documentId, document => document.WithNodeColour(node, colour));
    public RouteWorkspace UpdateNodeInfo(Guid documentId, WorkspaceNode? node, string? name, string? description) => Replace(documentId, document => document.WithNodeInfo(node, name, description));

    private RouteWorkspace Replace(Guid documentId, Func<WorkspaceDocument, WorkspaceDocument> update) => new(
        Id, Name, Documents.Select(document => document.Id == documentId ? update(document) : document), ActiveDocumentId, SelectedDocumentId);

    public RouteWorkspace Close(Guid documentId)
    {
        WorkspaceDocument[] remaining = Documents.Where(document => document.Id != documentId).ToArray();
        Guid? active = ActiveDocumentId == documentId ? remaining.FirstOrDefault()?.Id : ActiveDocumentId;
        Guid? selected = SelectedDocumentId == documentId ? null : SelectedDocumentId;
        return new RouteWorkspace(Id, Name, remaining, active, selected);
    }
}

public sealed class WorkspaceDocument
{
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

    public WorkspaceDocument WithVisibility(bool visible) => new(Id, Document, SourceFileName, visible, Colour,
        visible ? PresentationOverrides.Values.Select(item => item with { IsVisible = null }).Where(HasValue) : PresentationOverrides.Values);
    public WorkspaceDocument WithColour(string colour) => new(Id, Document, SourceFileName, IsVisible, colour, PresentationOverrides.Values);
    public WorkspaceDocument WithNodeVisibility(WorkspaceNode node, bool? visible)
    {
        var overrides = PresentationOverrides.Values
            .Where(item => visible is not true || !IsDescendantOrSelf(item.Node, node))
            .ToDictionary(item => item.Node);
        string? colour = overrides.GetValueOrDefault(node)?.Colour ?? PresentationOverrides.GetValueOrDefault(node)?.Colour;
        if (visible is not null || colour is not null) overrides[node] = new(node, visible, colour);
        return new WorkspaceDocument(Id, Document, SourceFileName, IsVisible, Colour, overrides.Values.Where(HasValue));
    }
    public WorkspaceDocument WithNodeColour(WorkspaceNode node, string? colour) => WithOverride(node, PresentationOverrides.GetValueOrDefault(node)?.IsVisible, colour);
    public bool IsNodeVisible(WorkspaceNode node) => IsVisible && InheritanceChain(node).Select(item => PresentationOverrides.GetValueOrDefault(item)?.IsVisible).LastOrDefault(value => value is not null) is not false;
    public string NodeColour(WorkspaceNode node) => InheritanceChain(node).Select(item => PresentationOverrides.GetValueOrDefault(item)?.Colour).LastOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? Colour;

    private WorkspaceDocument WithOverride(WorkspaceNode node, bool? visible, string? colour)
    {
        var overrides = PresentationOverrides.ToDictionary(pair => pair.Key, pair => pair.Value);
        if (visible is null && string.IsNullOrWhiteSpace(colour)) overrides.Remove(node);
        else overrides[node] = new NodePresentationOverride(node, visible, string.IsNullOrWhiteSpace(colour) ? null : colour);
        return new WorkspaceDocument(Id, Document, SourceFileName, IsVisible, Colour, overrides.Values);
    }

    public WorkspaceDocument WithNodeInfo(WorkspaceNode? node, string? name, string? description)
    {
        RouteDocument changed;
        if (node is null)
        {
            RouteMetadata existing = Document.Metadata ?? new RouteMetadata();
            changed = CopyDocument(metadata: new RouteMetadata(name, description, existing.Time, existing.Links, existing.Author, existing.UnsupportedExtensionXml));
        }
        else if (node.Value.Kind == WorkspaceNodeKind.Track)
        {
            Track[] tracks = [.. Document.Tracks]; Track old = tracks[node.Value.PrimaryIndex];
            tracks[node.Value.PrimaryIndex] = new Track(name, old.Segments, old.Type); changed = CopyDocument(tracks: tracks);
        }
        else if (node.Value.Kind == WorkspaceNodeKind.Route)
        {
            Route[] routes = [.. Document.Routes]; Route old = routes[node.Value.PrimaryIndex];
            routes[node.Value.PrimaryIndex] = new Route(name, old.Points, old.UnsupportedExtensionXml); changed = CopyDocument(routes: routes);
        }
        else if (node.Value.Kind == WorkspaceNodeKind.Waypoint)
        {
            Waypoint[] waypoints = [.. Document.Waypoints]; Waypoint old = waypoints[node.Value.PrimaryIndex];
            waypoints[node.Value.PrimaryIndex] = old with { Name = name, Description = description }; changed = CopyDocument(waypoints: waypoints);
        }
        else return this;
        return new WorkspaceDocument(Id, changed, SourceFileName, IsVisible, Colour, PresentationOverrides.Values);
    }

    private RouteDocument CopyDocument(IEnumerable<Track>? tracks = null, IEnumerable<Route>? routes = null, IEnumerable<Waypoint>? waypoints = null, RouteMetadata? metadata = null) =>
        new(tracks ?? Document.Tracks, routes ?? Document.Routes, waypoints ?? Document.Waypoints, metadata ?? Document.Metadata, Document.UnsupportedExtensionXml, Document.UnsupportedExtensionNamespaces);

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

public enum WorkspaceNodeKind { Track, Segment, Route, WaypointGroup, Waypoint }
public readonly record struct WorkspaceNode(WorkspaceNodeKind Kind, int PrimaryIndex = -1, int SecondaryIndex = -1);
public sealed record NodePresentationOverride(WorkspaceNode Node, bool? IsVisible = null, string? Colour = null);
