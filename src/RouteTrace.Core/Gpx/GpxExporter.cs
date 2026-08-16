using System.Text;
using System.Xml;
using RouteTrace.Core.Gpx.Writing;
using RouteTrace.Core.Routes.Documents;

namespace RouteTrace.Core.Gpx;

public static class GpxExporter
{
    public static async Task<GpxExportResult> ExportAsync(
        RouteDocument document,
        Stream output,
        string creator,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentException.ThrowIfNullOrWhiteSpace(creator);

        var settings = new XmlWriterSettings
        {
            Async = true,
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            Indent = true,
            CloseOutput = false
        };

        await using XmlWriter writer = XmlWriter.Create(output, settings);
        var documentWriter = new GpxDocumentWriter(
            writer,
            document,
            creator,
            cancellationToken);
        await documentWriter.WriteAsync();
        return new GpxExportResult(document.UnsupportedExtensionXml.Count, []);
    }
}
