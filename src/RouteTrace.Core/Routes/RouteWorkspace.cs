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
    public WorkspaceDocument(Guid id, RouteDocument document, string? sourceFileName = null, bool isVisible = true, string colour = "#2563eb")
    {
        if (id == Guid.Empty) throw new ArgumentException("A document ID is required.", nameof(id));
        Id = id;
        Document = document ?? throw new ArgumentNullException(nameof(document));
        SourceFileName = string.IsNullOrWhiteSpace(sourceFileName) ? null : sourceFileName;
        IsVisible = isVisible;
        Colour = string.IsNullOrWhiteSpace(colour) ? "#2563eb" : colour;
    }

    public Guid Id { get; }

    public RouteDocument Document { get; }

    public string? SourceFileName { get; }
    public bool IsVisible { get; }
    public string Colour { get; }

    public WorkspaceDocument WithVisibility(bool visible) => new(Id, Document, SourceFileName, visible, Colour);
}
