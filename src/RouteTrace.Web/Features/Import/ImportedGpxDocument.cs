using RouteTrace.Core.Routes.Documents;

namespace RouteTrace.Web.Features.Import;

public sealed record ImportedGpxDocument(RouteDocument Document, string SourceFileName);
