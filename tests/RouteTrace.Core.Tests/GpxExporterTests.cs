using System.Xml;
using System.Xml.Schema;
using System.Xml.Linq;
using RouteTrace.Core.Gpx;
using RouteTrace.Core.Routes.Documents;

namespace RouteTrace.Core.Tests;

public sealed class GpxExporterTests
{
    [Theory]
    [InlineData("FX-GPX-001-minimal-track.gpx")]
    [InlineData("FX-GPX-002-strava-wahoo-sanitised.gpx")]
    [InlineData("FX-GPX-003-multiple-tracks-segments.gpx")]
    [InlineData("FX-GPX-004-gpx-studio-supplemented.gpx")]
    [InlineData("FX-GPX-005-full-schema-surface.gpx")]
    public async Task ExportsSchemaValidGpxThatRetainsTheSupportedModel(string fixtureName)
    {
        RouteDocument original = await ImportFixture(fixtureName);

        await using var exported = new MemoryStream();
        GpxExportResult exportResult = await GpxExporter.ExportAsync(
            original, exported, "Route Trace tests", TestContext.Current.CancellationToken);

        exportResult.OmittedExtensionNamespaces.ShouldBeEmpty();
        exportResult.RetainedExtensionCount.ShouldBe(original.UnsupportedExtensionXml.Count);
        ValidateAgainstGpxSchema(exported.ToArray());

        exported.Position = 0;
        GpxImportResult importedAgain = await GpxImporter.ImportAsync(exported, TestContext.Current.CancellationToken);
        importedAgain.IsSuccess.ShouldBeTrue(importedAgain.Error);
        AssertSupportedModel(original, importedAgain.Document!);
    }

    [Fact]
    public async Task RequiresANonEmptyCreator()
    {
        var document = new RouteDocument();
        await using var output = new MemoryStream();

        await Should.ThrowAsync<ArgumentException>(() => GpxExporter.ExportAsync(document, output, " "));
    }

    [Fact]
    public async Task HonorsCancellationWhileWritingPreservedContent()
    {
        RouteDocument document = await ImportFixture("FX-GPX-005-full-schema-surface.gpx");
        await using var output = new MemoryStream();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(() =>
            GpxExporter.ExportAsync(document, output, "Route Trace tests", cancellation.Token));
    }

    [Fact]
    public async Task RestoresMetadataExtensionOwnershipAndWaypointAnnotations()
    {
        RouteDocument document = await ImportFixture("FX-GPX-004-gpx-studio-supplemented.gpx");
        await using var exported = new MemoryStream();

        await GpxExporter.ExportAsync(
            document, exported, "Route Trace tests", TestContext.Current.CancellationToken);

        XNamespace gpx = "http://www.topografix.com/GPX/1/1";
        exported.Position = 0;
        XDocument xml = await XDocument.LoadAsync(exported, LoadOptions.None, TestContext.Current.CancellationToken);
        XElement metadata = xml.Root!.Element(gpx + "metadata")!;
        metadata.Element(gpx + "author")!.Element(gpx + "name")!.Value.ShouldBe("gpx.studio");
        metadata.Element(gpx + "extensions")!.Elements().Single().Name.LocalName.ShouldBe("fixture");
        xml.Root.Element(gpx + "extensions").ShouldBeNull();
        xml.Root.Element(gpx + "rte")!.Element(gpx + "extensions")!
            .Elements().Single().Name.LocalName.ShouldBe("profile");

        XElement waypoint = xml.Root.Elements(gpx + "wpt").First();
        waypoint.Element(gpx + "cmt")!.Value.ShouldBe("This is a golf course.\nMake sure not to get hit.");
        waypoint.Element(gpx + "desc")!.Value.ShouldBe("This is a golf course.\nMake sure not to get hit.");
        waypoint.Element(gpx + "sym")!.Value.ShouldBe("Park");
        XElement linkedWaypoint = xml.Root.Elements(gpx + "wpt").ElementAt(1);
        linkedWaypoint.Element(gpx + "link")!.Attribute("href")!.Value.ShouldBe("https://www.bilka.dk/");
        xml.Descendants(gpx + "ele").ShouldAllBe(element => element.Value.Contains('.', StringComparison.Ordinal));
    }

    [Fact]
    public async Task PreservesTrackTypeAndTrackPointExtensionOwnershipAndFormatsCoordinates()
    {
        RouteDocument document = await ImportFixture("FX-GPX-002-strava-wahoo-sanitised.gpx");
        await using var exported = new MemoryStream();

        await GpxExporter.ExportAsync(
            document, exported, "Route Trace tests", TestContext.Current.CancellationToken);

        XNamespace gpx = "http://www.topografix.com/GPX/1/1";
        exported.Position = 0;
        XDocument xml = await XDocument.LoadAsync(exported, LoadOptions.None, TestContext.Current.CancellationToken);
        xml.Root!.GetNamespaceOfPrefix("gpxtpx")!.NamespaceName
            .ShouldBe("http://www.garmin.com/xmlschemas/TrackPointExtension/v1");
        XElement track = xml.Root!.Element(gpx + "trk")!;
        track.Element(gpx + "type")!.Value.ShouldBe("gravel_biking");
        XElement firstPoint = track.Element(gpx + "trkseg")!.Element(gpx + "trkpt")!;
        firstPoint.Attribute("lat")!.Value.ShouldBe("55.6582070");
        firstPoint.Attribute("lon")!.Value.ShouldBe("12.5394230");
        firstPoint.Element(gpx + "extensions")!.Elements().Single()
            .Name.LocalName.ShouldBe("TrackPointExtension");
        firstPoint.Element(gpx + "extensions")!.ToString(SaveOptions.DisableFormatting)
            .ShouldContain("xmlns:gpxtpx=\"http://www.garmin.com/xmlschemas/TrackPointExtension/v1\"");
        System.Text.Encoding.UTF8.GetString(exported.ToArray())
            .Split("xmlns:gpxtpx=", StringSplitOptions.None).Length.ShouldBe(2);
        xml.Root.Element(gpx + "extensions").ShouldBeNull();
    }

    private static void ValidateAgainstGpxSchema(byte[] xml)
    {
        var schemas = new XmlSchemaSet { XmlResolver = null };
        schemas.Add("http://www.topografix.com/GPX/1/1", TestDataPath("gpx-1.1.xsd"));
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            Schemas = schemas,
            ValidationType = ValidationType.Schema
        };
        settings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;
        settings.ValidationEventHandler += (_, eventArgs) =>
        {
            if (eventArgs.Severity == XmlSeverityType.Error)
            {
                throw new Xunit.Sdk.XunitException($"GPX schema validation failed: {eventArgs.Message}");
            }
        };

        using var input = new MemoryStream(xml, writable: false);
        using XmlReader reader = XmlReader.Create(input, settings);
        while (reader.Read()) { }
    }

    private static void AssertSupportedModel(RouteDocument expected, RouteDocument actual)
    {
        if (expected.Metadata is null)
        {
            actual.Metadata.ShouldBeNull();
        }
        else
        {
            actual.Metadata.ShouldNotBeNull();
            actual.Metadata.Name.ShouldBe(expected.Metadata.Name);
            actual.Metadata.Description.ShouldBe(expected.Metadata.Description);
            actual.Metadata.Time.ShouldBe(expected.Metadata.Time);
            actual.Metadata.Links.ShouldBe(expected.Metadata.Links);
            actual.Metadata.Author.ShouldBe(expected.Metadata.Author);
            actual.Metadata.UnsupportedExtensionXml.Count.ShouldBe(expected.Metadata.UnsupportedExtensionXml.Count);
        }
        actual.Waypoints.Count.ShouldBe(expected.Waypoints.Count);
        for (int index = 0; index < expected.Waypoints.Count; index++)
        {
            Waypoint expectedWaypoint = expected.Waypoints[index];
            Waypoint actualWaypoint = actual.Waypoints[index];
            actualWaypoint.Point.ShouldBe(expectedWaypoint.Point);
            actualWaypoint.Name.ShouldBe(expectedWaypoint.Name);
            actualWaypoint.Comment.ShouldBe(expectedWaypoint.Comment);
            actualWaypoint.Description.ShouldBe(expectedWaypoint.Description);
            actualWaypoint.Symbol.ShouldBe(expectedWaypoint.Symbol);
            actualWaypoint.Links.ShouldBe(expectedWaypoint.Links);
        }
        actual.Routes.Count.ShouldBe(expected.Routes.Count);
        for (int index = 0; index < expected.Routes.Count; index++)
        {
            actual.Routes[index].Name.ShouldBe(expected.Routes[index].Name);
            actual.Routes[index].Points.ShouldBe(expected.Routes[index].Points);
            actual.Routes[index].UnsupportedExtensionXml.Count
                .ShouldBe(expected.Routes[index].UnsupportedExtensionXml.Count);
        }

        actual.Tracks.Count.ShouldBe(expected.Tracks.Count);
        for (int trackIndex = 0; trackIndex < expected.Tracks.Count; trackIndex++)
        {
            Track expectedTrack = expected.Tracks[trackIndex];
            Track actualTrack = actual.Tracks[trackIndex];
            actualTrack.Name.ShouldBe(expectedTrack.Name);
            actualTrack.Type.ShouldBe(expectedTrack.Type);
            actualTrack.Segments.Count.ShouldBe(expectedTrack.Segments.Count);
            for (int segmentIndex = 0; segmentIndex < expectedTrack.Segments.Count; segmentIndex++)
            {
                actualTrack.Segments[segmentIndex].Points.ShouldBe(expectedTrack.Segments[segmentIndex].Points);
            }
        }

        actual.UnsupportedExtensionNamespaces.ShouldBe(expected.UnsupportedExtensionNamespaces);
        actual.UnsupportedExtensionXml.Count.ShouldBe(expected.UnsupportedExtensionXml.Count);
    }

    private static async Task<RouteDocument> ImportFixture(string name)
    {
        await using FileStream stream = File.OpenRead(TestDataPath(name));
        GpxImportResult result = await GpxImporter.ImportAsync(stream, TestContext.Current.CancellationToken);
        result.IsSuccess.ShouldBeTrue(result.Error);
        return result.Document!;
    }

    private static string TestDataPath(string name) => Path.Combine(AppContext.BaseDirectory, "TestData", name);
}
