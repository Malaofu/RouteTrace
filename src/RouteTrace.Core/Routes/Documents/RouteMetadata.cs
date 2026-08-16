namespace RouteTrace.Core.Routes.Documents;

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

public sealed record RouteAuthor(
    string? Name = null,
    string? EmailId = null,
    string? EmailDomain = null,
    RouteLink? Link = null);
