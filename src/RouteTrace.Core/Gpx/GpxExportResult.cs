namespace RouteTrace.Core.Gpx;

public sealed record GpxExportResult(
    int RetainedExtensionCount,
    IReadOnlyList<string> OmittedExtensionNamespaces);
