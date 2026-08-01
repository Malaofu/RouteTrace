using RouteTrace.Web.Features.Import;

namespace RouteTrace.Web.Tests;

public sealed class GpxDownloadFileNameTests
{
    [Theory]
    [InlineData("Morning ride", "original.gpx", "Morning ride.gpx")]
    [InlineData("Morning ride.GPX", "original.gpx", "Morning ride.GPX")]
    [InlineData("A/B:C*D?", "original.gpx", "A-B-C-D-.gpx")]
    [InlineData(null, "original.gpx", "original.gpx")]
    [InlineData(" ", "original.gpx", "original.gpx")]
    [InlineData(null, null, "route-trace.gpx")]
    public void UsesMetadataNameThenImportedFileName(string? documentName, string? importedFileName, string expected)
    {
        GpxDownloadFileName.From(documentName, importedFileName).ShouldBe(expected);
    }
}
