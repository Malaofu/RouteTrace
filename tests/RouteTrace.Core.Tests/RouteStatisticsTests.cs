using RouteTrace.Core.Gpx;
using RouteTrace.Core.Routes.Analysis;
using RouteTrace.Core.Routes.Documents;
using RouteTrace.Core.Routes.Geometry;

namespace RouteTrace.Core.Tests;

public sealed class RouteStatisticsTests
{
    [Fact]
    public void DoesNotMeasureAcrossSegmentGaps()
    {
        var document = new RouteDocument(tracks: [new Track(segments:
        [
            new TrackSegment([Point(0, 0), Point(0, 0.001)]),
            new TrackSegment([Point(50, 50), Point(50, 50.001)])
        ])]);

        RouteStatistics statistics = RouteStatisticsCalculator.Calculate(document);

        statistics.Segments.Count.ShouldBe(2);
        statistics.Segments[0].DistanceMetres.ShouldBe(111.195, 0.01);
        statistics.Segments[1].DistanceMetres.ShouldBe(71.475, 0.01);
        statistics.TotalTrackDistanceMetres.ShouldBe(
            statistics.Segments.Sum(segment => segment.DistanceMetres), 0.0001);
        statistics.TotalTrackDistanceMetres.ShouldBeLessThan(250);
    }

    [Fact]
    public async Task ReportsCompletePartialAndAbsentElevationMeaningfully()
    {
        GpxImportResult result = await ImportFixture("FX-ELE-001-elevation-coverage.gpx");
        result.IsSuccess.ShouldBeTrue(result.Error);
        RouteDocument document = result.Document!;

        RouteStatistics complete = RouteStatisticsCalculator.Calculate(
            new RouteDocument(tracks: [document.Tracks[0]]));
        RouteStatistics partial = RouteStatisticsCalculator.Calculate(
            new RouteDocument(tracks: [document.Tracks[1]]));
        RouteStatistics absent = RouteStatisticsCalculator.Calculate(
            new RouteDocument(tracks: [document.Tracks[2]]));

        complete.Elevation.ShouldBe(new ElevationStatistics(10, 20, 13, 3, true));
        partial.Elevation.ShouldBe(new ElevationStatistics(5, 9, null, null, false));
        absent.Elevation.ShouldBeNull();
    }

    [Fact]
    public void ReportsTimeRangeAndMissingTimeWithoutInventingZeroes()
    {
        DateTimeOffset start = DateTimeOffset.Parse("2020-01-01T09:00:00Z");
        var timed = new RouteDocument(tracks: [new Track(segments: [new TrackSegment(
            [Point(0, 0, time: start), Point(0, 0.001, time: start.AddMinutes(3))])])]);

        RouteStatisticsCalculator.Calculate(timed).Time.ShouldBe(
            new TimeStatistics(start, start.AddMinutes(3), TimeSpan.FromMinutes(3)));
        RouteStatisticsCalculator.Calculate(new RouteDocument()).Time.ShouldBeNull();
    }

    [Fact]
    public void IdentifiesDistinctExtensionNamespaces()
    {
        var document = new RouteDocument(unsupportedExtensionXml:
        [
            "<a:item xmlns:a='urn:first'/>",
            "<a:other xmlns:a='urn:first'/>",
            "<b:item xmlns:b='urn:second'/>"
        ]);

        RouteStatisticsCalculator.Calculate(document).ExtensionNamespaces
            .ShouldBe(["urn:first", "urn:second"]);
    }

    private static RoutePoint Point(
        double latitude,
        double longitude,
        double? elevation = null,
        DateTimeOffset? time = null) =>
        new(new GeoCoordinate(latitude, longitude), elevation, time);

    private static async Task<GpxImportResult> ImportFixture(string name)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "TestData", name);
        await using FileStream stream = File.OpenRead(path);
        return await GpxImporter.ImportAsync(stream, TestContext.Current.CancellationToken);
    }
}
