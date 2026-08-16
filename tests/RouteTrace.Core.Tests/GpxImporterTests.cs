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
        result.Document.Metadata.Author.ShouldBe(new RouteAuthor(
            "gpx.studio", Link: new RouteLink("https://gpx.studio")));
        result.Document.Metadata.UnsupportedExtensionXml.Count.ShouldBe(1);
        result.Document.Waypoints.Select(waypoint => waypoint.Name)
            .ShouldBe(["Golf Course", "Shopping", "Trees", "Parking"]);
        result.Document.Waypoints[0].Comment.ShouldBe("This is a golf course.\nMake sure not to get hit.");
        result.Document.Waypoints[0].Description.ShouldBe("This is a golf course.\nMake sure not to get hit.");
        result.Document.Waypoints[0].Symbol.ShouldBe("Park");
        result.Document.Waypoints[1].Links.ShouldBe([new RouteLink("https://www.bilka.dk/")]);
        result.Document.Routes.Single().UnsupportedExtensionXml.Count.ShouldBe(1);
        result.Document.UnsupportedExtensionXml.Count.ShouldBe(2);
        result.Document.UnsupportedExtensionXml.ShouldAllBe(xml => xml.Contains("https://routetrace.app/xmlschemas/fixture/v1"));
    }

    [Fact]
    public async Task ImportsEndpointAndSymbolCoverageFixture()
    {
        GpxImportResult result = await ImportFixture("FX-GPX-006-endpoints-and-symbols.gpx");

        result.IsSuccess.ShouldBeTrue(result.Error);
        result.Document!.Tracks.Count.ShouldBe(2);
        result.Document.Tracks[0].Segments.Count.ShouldBe(2);
        result.Document.Routes.Count.ShouldBe(2);
        result.Document.Waypoints.Select(waypoint => waypoint.Symbol)
            .ShouldBe(["Park", "Parking Area", "Vendor Mystery 42", null]);
    }

    [Fact]
    public async Task StreamsFullDensitySanitisedPerformanceFixture()
    {
        GpxImportResult result = await ImportFixture("FX-GPX-002-a-strava-wahoo-full-density-sanitised.gpx");
        RouteStatistics statistics = RouteStatisticsCalculator.Calculate(result.Document!);

        result.IsSuccess.ShouldBeTrue(result.Error);
        TrackSegment segment = result.Document!.Tracks.Single().Segments.Single();
        segment.Points.Count.ShouldBe(6987);
        segment.Points[0].Time.ShouldBe(DateTimeOffset.Parse("2020-01-01T09:00:00Z"));
        segment.Points[^1].Time.ShouldBe(DateTimeOffset.Parse("2020-01-01T11:09:36Z"));
        result.Document.UnsupportedExtensionXml.Count.ShouldBe(6987);
        statistics.ExtensionNamespaces.ShouldBe(["http://www.garmin.com/xmlschemas/TrackPointExtension/v1"]);
    }

    [Fact]
    public async Task ImportsFromAsyncOnlyBrowserStyleStream()
    {
        string path = Path.Combine(
            AppContext.BaseDirectory, "TestData", "FX-GPX-002-a-strava-wahoo-full-density-sanitised.gpx");
        await using FileStream file = File.OpenRead(path);
        await using var stream = new AsyncOnlyStream(file);

        GpxImportResult result = await GpxImporter.ImportAsync(stream, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.Error);
        result.Document!.Tracks.Single().Segments.Single().Points.Count.ShouldBe(6987);
    }

    [Fact]
    public async Task ImportsSanitisedRealExporterFixtureAndPreservesGarminExtensions()
    {
        GpxImportResult result = await ImportFixture("FX-GPX-002-strava-wahoo-sanitised.gpx");

        result.IsSuccess.ShouldBeTrue(result.Error);
        result.Document!.Tracks.Single().Segments.Single().Points.Count.ShouldBe(196);
        result.Document.Tracks.Single().Type.ShouldBe("gravel_biking");
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

    private sealed class AsyncOnlyStream(Stream inner) : Stream
    {
        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => inner.CanSeek;
        public override bool CanWrite => false;
        public override long Length => inner.Length;
        public override long Position { get => inner.Position; set => inner.Position = value; }
        public override void Flush() => throw new NotSupportedException();
        public override int Read(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException("Synchronous reads are not supported.");
        public override int Read(Span<byte> buffer) =>
            throw new NotSupportedException("Synchronous reads are not supported.");
        public override int ReadByte() =>
            throw new NotSupportedException("Synchronous reads are not supported.");
        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default) => inner.ReadAsync(buffer, cancellationToken);
        public override Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken) => inner.ReadAsync(buffer, offset, count, cancellationToken);
        public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        protected override void Dispose(bool disposing)
        {
            if (disposing) inner.Dispose();
            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            await inner.DisposeAsync();
            GC.SuppressFinalize(this);
        }
    }
}
