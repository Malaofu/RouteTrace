using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using RouteTrace.Core.Gpx;
using RouteTrace.Core.Routes.Documents;
using RouteTrace.Core.Routes.Workspaces;
using RouteTrace.Web.Features.Import;

namespace RouteTrace.Web.Tests;

public sealed class GpxOperationsTests
{
    [Fact]
    public async Task ImportReturnsDocumentFeedbackAndUsesSharedSizeLimit()
    {
        var browserFile = new TestBrowserFile(
            "ride.gpx",
            """
            <?xml version="1.0" encoding="UTF-8"?>
            <gpx version="1.1" creator="test" xmlns="http://www.topografix.com/GPX/1/1">
              <trk><trkseg><trkpt lat="55.67" lon="12.56" /></trkseg></trk>
            </gpx>
            """);
        var operation = new GpxImportOperation();

        GpxImportOutcome outcome = await operation.ExecuteAsync(
            browserFile,
            TestContext.Current.CancellationToken);

        outcome.IsError.ShouldBeFalse();
        outcome.ImportedDocument.ShouldNotBeNull();
        outcome.ImportedDocument.SourceFileName.ShouldBe("ride.gpx");
        outcome.Message.ShouldBe("Imported ride.gpx: 1 point(s).");
        browserFile.LastMaximumAllowedSize.ShouldBe(GpxImportOperation.MaximumFileSize);
    }

    [Fact]
    public async Task ImportReturnsParserFailureWithoutDocument()
    {
        var operation = new GpxImportOperation();

        GpxImportOutcome outcome = await operation.ExecuteAsync(
            new TestBrowserFile("invalid.gpx", "not GPX"),
            TestContext.Current.CancellationToken);

        outcome.IsError.ShouldBeTrue();
        outcome.ImportedDocument.ShouldBeNull();
        outcome.Message.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ImportTurnsReadFailureIntoUserFeedback()
    {
        var operation = new GpxImportOperation();

        GpxImportOutcome outcome = await operation.ExecuteAsync(
            new TestBrowserFile("large.gpx", new IOException("Too large.")),
            TestContext.Current.CancellationToken);

        outcome.IsError.ShouldBeTrue();
        outcome.ImportedDocument.ShouldBeNull();
        outcome.Message.ShouldBe("The file could not be read: Too large.");
    }

    [Fact]
    public async Task ExportUsesSharedFilenameAndReturnsNotice()
    {
        var javaScript = new RecordingJavaScript();
        await using var operation = new GpxExportOperation(javaScript);
        var target = new WorkspaceDocument(
            Guid.NewGuid(),
            new RouteDocument(metadata: new RouteMetadata(name: "Morning/Ride")),
            "original.gpx");

        GpxExportOutcome outcome = await operation.ExecuteAsync(
            target,
            TestContext.Current.CancellationToken);

        javaScript.Download.FileName.ShouldBe("Morning-Ride.gpx");
        outcome.Notice.ShouldBe("Downloaded GPX with 0 retained extension element(s).");
    }

    [Fact]
    public void ExportNoticeReportsOmittedNamespaces()
    {
        var result = new GpxExportResult(2, ["urn:vendor:first", "urn:vendor:second"]);

        GpxExportOutcome.From(result).Notice.ShouldBe(
            "Downloaded GPX; omitted: urn:vendor:first, urn:vendor:second.");
    }

    private sealed class TestBrowserFile : IBrowserFile
    {
        private readonly byte[]? contents;
        private readonly IOException? exception;

        public TestBrowserFile(string name, string contents)
        {
            Name = name;
            this.contents = System.Text.Encoding.UTF8.GetBytes(contents);
            Size = this.contents.Length;
        }

        public TestBrowserFile(string name, IOException exception)
        {
            Name = name;
            this.exception = exception;
        }

        public string Name { get; }
        public DateTimeOffset LastModified => DateTimeOffset.UnixEpoch;
        public long Size { get; }
        public string ContentType => "application/gpx+xml";
        public long? LastMaximumAllowedSize { get; private set; }

        public Stream OpenReadStream(
            long maxAllowedSize = 512000,
            CancellationToken cancellationToken = default)
        {
            LastMaximumAllowedSize = maxAllowedSize;
            if (exception is not null)
            {
                throw exception;
            }

            return new MemoryStream(contents!, writable: false);
        }
    }

    private sealed class RecordingJavaScript : IJSRuntime
    {
        public RecordingDownloadModule Download { get; } = new();

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args)
        {
            object? result = identifier == "import" ? Download : default(TValue);
            return ValueTask.FromResult((TValue)result!);
        }
    }

    private sealed class RecordingDownloadModule : IJSObjectReference
    {
        public string? FileName { get; private set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args)
        {
            if (identifier == "downloadStream")
            {
                FileName = (string?)args?[0];
            }

            return ValueTask.FromResult(default(TValue)!);
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
