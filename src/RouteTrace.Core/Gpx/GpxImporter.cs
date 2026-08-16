using System.Xml;
using RouteTrace.Core.Gpx.Parsing;
using RouteTrace.Core.Gpx.Preservation;

namespace RouteTrace.Core.Gpx;

public static class GpxImporter
{
    public static async Task<GpxImportResult> ImportAsync(
        Stream input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        try
        {
            await using var bufferedInput = new MemoryStream();
            await input.CopyToAsync(bufferedInput, cancellationToken);
            byte[] sourceXml = bufferedInput.ToArray();
            var preservedXml = new LazyExtensionXml(sourceXml);
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null
            };

            using var parseInput = new MemoryStream(sourceXml, writable: false);
            using XmlReader reader = XmlReader.Create(parseInput, settings);
            return new GpxStreamParser(reader, preservedXml, cancellationToken).Parse();
        }
        catch (XmlException exception)
        {
            return GpxImportResult.Failure($"The file is not valid XML: {exception.Message}");
        }
        catch (InvalidDataException exception)
        {
            return GpxImportResult.Failure(exception.Message);
        }
    }
}
