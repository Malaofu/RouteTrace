using System.Collections;
using System.Xml.Linq;

namespace RouteTrace.Core.Gpx;

internal sealed class LazyExtensionXml(byte[] sourceXml) : IReadOnlyList<string>
{
    private static readonly XNamespace Gpx = "http://www.topografix.com/GPX/1/1";
    private readonly Lazy<string[]> values = new(() =>
    {
        using var input = new MemoryStream(sourceXml, writable: false);
        XDocument document = XDocument.Load(input, LoadOptions.None);
        return document.Descendants(Gpx + "extensions")
            .Elements()
            .Where(element => element.Name.Namespace != Gpx)
            .Select(element => element.ToString(SaveOptions.DisableFormatting))
            .ToArray();
    });

    public int Count => values.Value.Length;

    public string this[int index] => values.Value[index];

    public IEnumerator<string> GetEnumerator() => ((IEnumerable<string>)values.Value).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
