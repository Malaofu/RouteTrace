using System.Globalization;
using System.Xml;
using RouteTrace.Core.Gpx;

namespace RouteTrace.Core.Gpx.Writing;

internal static class GpxWriterFormatting
{
    public static string FormatElevation(double elevation)
    {
        string value = elevation.ToString("R", CultureInfo.InvariantCulture);
        int exponentIndex = value.IndexOfAny(['E', 'e']);
        string significand = exponentIndex < 0 ? value : value[..exponentIndex];
        if (significand.Contains('.', StringComparison.Ordinal))
        {
            return value;
        }

        return exponentIndex < 0
            ? value + ".0"
            : value.Insert(exponentIndex, ".0");
    }

    public static Task WriteOptionalElementAsync(
        XmlWriter writer,
        string name,
        string? value) =>
        string.IsNullOrEmpty(value)
            ? Task.CompletedTask
            : writer.WriteElementStringAsync(
                null,
                name,
                GpxXml.NamespaceName,
                value);
}
