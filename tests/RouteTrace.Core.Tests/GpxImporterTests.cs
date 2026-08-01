using System.Text;
using RouteTrace.Core.Gpx;
using RouteTrace.Core.Routes;

namespace RouteTrace.Core.Tests;

public sealed class GpxImporterTests
{
    [Fact]
    public async Task ImportsMinimalTrackWithMetadataElevationAndTimes()
    {
        GpxImportResult result = await ImportFixture("FX-GPX-001-minimal-track.gpx");

        result.IsSuccess.ShouldBeTrue(result.Error);
        result.Document!.Metadata!.Name.ShouldBe("Minimal elevated track");
        result.Document.Metadata.Time.ShouldBe(DateTimeOffset.Parse("2020-01-01T08:00:00Z"));
        TrackSegment segment = result.Document.Tracks.Single().Segments.Single();
        segment.Points.Count.ShouldBe(3);
        segment.Points[0].ElevationMetres.ShouldBe(12.5);
        segment.Points[2].Time.ShouldBe(DateTimeOffset.Parse("2020-01-01T08:02:00Z"));
    }

    [Fact]
    public async Task PreservesMultipleTracksAndSegments()
    {
        GpxImportResult result = await ImportFixture("FX-GPX-003-multiple-tracks-segments.gpx");

        result.IsSuccess.ShouldBeTrue(result.Error);
        result.Document!.Tracks.Count.ShouldBe(2);
        result.Document.Tracks[0].Segments.Count.ShouldBe(2);
        result.Document.Tracks[1].Segments.Count.ShouldBe(1);
    }

    [Fact]
    public async Task ImportsRoutesWaypointsAndUnsupportedExtensions()
    {
        GpxImportResult result = await ImportFixture("FX-GPX-004-gpx-studio-supplemented.gpx");

        result.IsSuccess.ShouldBeTrue(result.Error);
        result.Document!.Routes.Single().Points.Count.ShouldBe(5);
        result.Document.Metadata!.Name.ShouldBe("Test GPX");
        result.Document.Metadata.Description.ShouldBe("This is a simple test pgx file");
        result.Document.Waypoints.Select(waypoint => waypoint.Name)
            .ShouldBe(["Golf Course", "Shopping", "Trees", "Parking"]);
        result.Document.UnsupportedExtensionXml.Count.ShouldBe(2);
        result.Document.UnsupportedExtensionXml.ShouldAllBe(xml => xml.Contains("https://routetrace.app/xmlschemas/fixture/v1"));
    }

    [Fact]
    public async Task ImportsFullDensitySanitisedPerformanceFixture()
    {
        GpxImportResult result = await ImportFixture("FX-GPX-002-a-strava-wahoo-full-density-sanitised.gpx");

        result.IsSuccess.ShouldBeTrue(result.Error);
        TrackSegment segment = result.Document!.Tracks.Single().Segments.Single();
        segment.Points.Count.ShouldBe(6987);
        segment.Points[0].Time.ShouldBe(DateTimeOffset.Parse("2020-01-01T09:00:00Z"));
        segment.Points[^1].Time.ShouldBe(DateTimeOffset.Parse("2020-01-01T11:09:36Z"));
        result.Document.UnsupportedExtensionXml.Count.ShouldBe(6987);
    }

    [Fact]
    public async Task ImportsSanitisedRealExporterFixtureAndPreservesGarminExtensions()
    {
        GpxImportResult result = await ImportFixture("FX-GPX-002-strava-wahoo-sanitised.gpx");

        result.IsSuccess.ShouldBeTrue(result.Error);
        result.Document!.Tracks.Single().Segments.Single().Points.Count.ShouldBe(196);
        result.Document.UnsupportedExtensionXml.ShouldNotBeEmpty();
        result.Document.UnsupportedExtensionXml.ShouldContain(xml => xml.Contains("TrackPointExtension"));
    }

    [Theory]
    [InlineData("<gpx>")]
    [InlineData("<gpx xmlns='http://www.topografix.com/GPX/1/1' version='1.1'><trk><trkseg><trkpt lat='91' lon='0'/></trkseg></trk></gpx>")]
    [InlineData("<gpx xmlns='http://www.topografix.com/GPX/1/1' version='1.0'/>")]
    public async Task ReturnsReadableFailureForInvalidInput(string xml)
    {
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

        GpxImportResult result = await GpxImporter.ImportAsync(stream, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Document.ShouldBeNull();
        result.Error.ShouldNotBeNullOrWhiteSpace();
    }

    private static async Task<GpxImportResult> ImportFixture(string name)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "TestData", name);
        await using FileStream stream = File.OpenRead(path);
        return await GpxImporter.ImportAsync(stream, TestContext.Current.CancellationToken);
    }
}
