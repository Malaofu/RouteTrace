using RouteTrace.Core.Routes.Documents;

namespace RouteTrace.Core.Gpx;

public sealed record GpxImportResult(RouteDocument? Document, string? Error)
{
    public bool IsSuccess => Document is not null;

    public static GpxImportResult Success(RouteDocument document) => new(document, null);

    public static GpxImportResult Failure(string error) => new(null, error);
}
