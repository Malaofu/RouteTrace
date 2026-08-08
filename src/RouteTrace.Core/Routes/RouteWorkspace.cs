namespace RouteTrace.Core.Routes;

public sealed class RouteWorkspace
{
    public RouteWorkspace(
        Guid id,
        string name,
        IEnumerable<WorkspaceDocument>? documents = null,
        Guid? activeDocumentId = null)
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

        Id = id;
        Name = name.Trim();
        Documents = Array.AsReadOnly(documentSnapshot);
        ActiveDocumentId = activeDocumentId;
    }

    public Guid Id { get; }

    public string Name { get; }

    public IReadOnlyList<WorkspaceDocument> Documents { get; }

    public Guid? ActiveDocumentId { get; }

    public WorkspaceDocument? ActiveDocument =>
        ActiveDocumentId is null ? null : Documents.Single(document => document.Id == ActiveDocumentId);

    public RouteWorkspace AddDocument(RouteDocument document, string? sourceFileName = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        var workspaceDocument = new WorkspaceDocument(Guid.NewGuid(), document, sourceFileName);
        return new RouteWorkspace(Id, Name, Documents.Append(workspaceDocument), workspaceDocument.Id);
    }

    public RouteWorkspace Rename(string name) => new(Id, name, Documents, ActiveDocumentId);

    public RouteWorkspace Activate(Guid documentId) => new(Id, Name, Documents, documentId);
}

public sealed class WorkspaceDocument
{
    public WorkspaceDocument(Guid id, RouteDocument document, string? sourceFileName = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("A document ID is required.", nameof(id));
        Id = id;
        Document = document ?? throw new ArgumentNullException(nameof(document));
        SourceFileName = string.IsNullOrWhiteSpace(sourceFileName) ? null : sourceFileName;
    }

    public Guid Id { get; }

    public RouteDocument Document { get; }

    public string? SourceFileName { get; }
}
