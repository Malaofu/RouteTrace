using RouteTrace.Core.Routes.Documents;

namespace RouteTrace.Core.Routes.Workspaces;

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
        var workspaceDocument = new WorkspaceDocument(
            Guid.NewGuid(), document, sourceFileName, true, WorkspaceDocument.DefaultColour(Documents.Count));
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
