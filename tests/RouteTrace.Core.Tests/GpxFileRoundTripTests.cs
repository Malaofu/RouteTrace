using System.Globalization;
using System.Xml.Linq;
using RouteTrace.Core.Gpx;

namespace RouteTrace.Core.Tests;

public sealed class GpxFileRoundTripTests
{
    [Theory]
    [InlineData("FX-GPX-001-minimal-track.gpx")]
    [InlineData("FX-GPX-002-strava-wahoo-sanitised.gpx")]
    [InlineData("FX-GPX-002-a-strava-wahoo-full-density-sanitised.gpx")]
    [InlineData("FX-GPX-003-multiple-tracks-segments.gpx")]
    [InlineData("FX-GPX-004-gpx-studio-supplemented.gpx")]
    [InlineData("FX-GPX-005-full-schema-surface.gpx")]
    [InlineData("FX-ELE-001-elevation-coverage.gpx")]
    public async Task ImportExportPreservesTheGpxFileExceptCreatorAndNumericFormatting(string fixtureName)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "TestData", fixtureName);
        await using FileStream input = File.OpenRead(path);
        GpxImportResult imported = await GpxImporter.ImportAsync(input, TestContext.Current.CancellationToken);
        imported.IsSuccess.ShouldBeTrue(imported.Error);

        await using var output = new MemoryStream();
        await GpxExporter.ExportAsync(
            imported.Document!, output, "Route Trace tests", TestContext.Current.CancellationToken);

        await using FileStream expectedInput = File.OpenRead(path);
        XDocument expected = await XDocument.LoadAsync(
            expectedInput, LoadOptions.None, TestContext.Current.CancellationToken);
        output.Position = 0;
        XDocument actual = await XDocument.LoadAsync(output, LoadOptions.None, TestContext.Current.CancellationToken);
        expected.Root!.SetAttributeValue("creator", "Route Trace tests");

        AssertEquivalent(expected.Root, actual.Root!, "/gpx");
        XNamespace gpx = "http://www.topografix.com/GPX/1/1";
        actual.Descendants()
            .Where(element => element.Name == gpx + "wpt" || element.Name == gpx + "rtept" || element.Name == gpx + "trkpt")
            .SelectMany(element => new[] { element.Attribute("lat")!.Value, element.Attribute("lon")!.Value })
            .ShouldAllBe(value => HasSevenFractionalDigits(value));
    }

    private static void AssertEquivalent(XElement expected, XElement actual, string path)
    {
        actual.Name.ShouldBe(expected.Name, $"Element name differs at {path}.");

        Dictionary<XName, string> expectedAttributes = expected.Attributes()
            .ToDictionary(attribute => attribute.Name, attribute => attribute.Value);
        Dictionary<XName, string> actualAttributes = actual.Attributes()
            .ToDictionary(attribute => attribute.Name, attribute => attribute.Value);
        actualAttributes.Keys.ShouldBe(
            expectedAttributes.Keys,
            ignoreOrder: true,
            customMessage: $"Attributes differ at {path}. Actual: {string.Join(", ", actual.Attributes().Select(attribute => $"{attribute.Name}='{attribute.Value}'"))}");
        foreach ((XName name, string expectedValue) in expectedAttributes)
        {
            ValuesAreEquivalent(expectedValue, actualAttributes[name], IsDecimalAttribute(name)).ShouldBeTrue(
                $"Attribute {name} differs at {path}: expected '{expectedValue}', actual '{actualAttributes[name]}'.");
        }

        List<XElement> expectedChildren = expected.Elements().ToList();
        List<XElement> actualChildren = actual.Elements().ToList();
        actualChildren.Count.ShouldBe(expectedChildren.Count, $"Child count differs at {path}.");
        for (int index = 0; index < expectedChildren.Count; index++)
        {
            XElement expectedChild = expectedChildren[index];
            AssertEquivalent(expectedChild, actualChildren[index], $"{path}/{expectedChild.Name.LocalName}[{index + 1}]");
        }

        if (expectedChildren.Count == 0)
        {
            ValuesAreEquivalent(expected.Value, actual.Value, IsDecimalElement(expected.Name)).ShouldBeTrue(
                $"Value differs at {path}: expected '{expected.Value}', actual '{actual.Value}'.");
        }
    }

    private static bool ValuesAreEquivalent(string expected, string actual, bool allowNumericFormatting)
    {
        if (string.Equals(expected, actual, StringComparison.Ordinal)) return true;
        if (!allowNumericFormatting) return false;

        return double.TryParse(expected, NumberStyles.Float, CultureInfo.InvariantCulture, out double expectedNumber)
            && double.TryParse(actual, NumberStyles.Float, CultureInfo.InvariantCulture, out double actualNumber)
            && expectedNumber.Equals(actualNumber);
    }

    private static bool IsDecimalAttribute(XName name) =>
        name.NamespaceName.Length == 0
        && name.LocalName is "lat" or "lon" or "minlat" or "minlon" or "maxlat" or "maxlon";

    private static bool IsDecimalElement(XName name) =>
        name.NamespaceName == "http://www.topografix.com/GPX/1/1"
        && name.LocalName is "ele" or "magvar" or "geoidheight" or "hdop" or "vdop" or "pdop" or "ageofdgpsdata";

    private static bool HasSevenFractionalDigits(string value)
    {
        int decimalPoint = value.IndexOf('.');
        return decimalPoint >= 0 && value.Length - decimalPoint - 1 == 7;
    }
}
