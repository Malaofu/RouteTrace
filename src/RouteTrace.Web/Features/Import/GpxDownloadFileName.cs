using System.Buffers;

namespace RouteTrace.Web.Features.Import;

public static class GpxDownloadFileName
{
    private const string DefaultFileName = "route-trace.gpx";
    private static readonly SearchValues<char> InvalidCharacters = SearchValues.Create("<>:\"/\\|?*");

    public static string From(string? documentName, string? importedFileName)
    {
        string candidate = !string.IsNullOrWhiteSpace(documentName)
            ? documentName
            : !string.IsNullOrWhiteSpace(importedFileName)
                ? importedFileName
                : DefaultFileName;
        string sanitized = new string(candidate
            .Trim()
            .Select(character => character < ' ' || InvalidCharacters.Contains(character) ? '-' : character)
            .ToArray()).TrimEnd('.', ' ');
        if (string.IsNullOrWhiteSpace(sanitized)) return DefaultFileName;

        return sanitized.EndsWith(".gpx", StringComparison.OrdinalIgnoreCase)
            ? sanitized
            : sanitized + ".gpx";
    }
}
