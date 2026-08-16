using System.Xml;
using System.Xml.Linq;
using RouteTrace.Core.Gpx;
using RouteTrace.Core.Gpx.Preservation;
using RouteTrace.Core.Routes.Documents;

namespace RouteTrace.Core.Gpx.Writing;

internal sealed class GpxDocumentWriter(
    XmlWriter writer,
    RouteDocument document,
    string creator,
    CancellationToken cancellationToken)
{
    private readonly LazyExtensionXml? preservedXml =
        document.UnsupportedExtensionXml as LazyExtensionXml;

    public async Task WriteAsync()
    {
        await writer.WriteStartDocumentAsync();
        await writer.WriteStartElementAsync(null, "gpx", GpxXml.NamespaceName);
        await writer.WriteAttributeStringAsync(null, "version", null, "1.1");
        await writer.WriteAttributeStringAsync(null, "creator", null, creator);
        await GpxPreservedContentWriter.WriteRootAttributesAsync(writer, preservedXml);

        if (document.Metadata is not null)
        {
            await GpxSchemaElementWriter.WriteMetadataAsync(
                writer,
                document.Metadata,
                preservedXml,
                cancellationToken);
        }

        await WriteWaypointsAsync();
        await WriteRoutesAsync();
        await WriteTracksAsync();
        await GpxPreservedContentWriter.WriteDocumentExtensionsAsync(
            writer,
            document,
            preservedXml,
            cancellationToken);

        await writer.WriteEndElementAsync();
        await writer.WriteEndDocumentAsync();
        await writer.FlushAsync();
    }

    private async Task WriteWaypointsAsync()
    {
        for (int waypointIndex = 0; waypointIndex < document.Waypoints.Count; waypointIndex++)
        {
            Waypoint waypoint = document.Waypoints[waypointIndex];
            await GpxSchemaElementWriter.WritePointAsync(
                writer,
                "wpt",
                waypoint.Point,
                waypoint.Name,
                waypoint.Comment,
                waypoint.Description,
                waypoint.Symbol,
                waypoint.Links,
                preservedXml?.At(GpxExtensionScope.Waypoint, waypointIndex),
                preservedXml?.StandardChildrenAt(GpxExtensionScope.Waypoint, waypointIndex),
                cancellationToken);
        }
    }

    private async Task WriteRoutesAsync()
    {
        for (int routeIndex = 0; routeIndex < document.Routes.Count; routeIndex++)
        {
            Route route = document.Routes[routeIndex];
            await writer.WriteStartElementAsync(null, "rte", GpxXml.NamespaceName);
            await WriteRouteHeaderAsync(route, routeIndex);

            for (int pointIndex = 0; pointIndex < route.Points.Count; pointIndex++)
            {
                await GpxSchemaElementWriter.WritePointAsync(
                    writer,
                    "rtept",
                    route.Points[pointIndex],
                    null,
                    extensions: preservedXml?.At(
                        GpxExtensionScope.RoutePoint,
                        routeIndex,
                        pointIndex),
                    standardChildren: preservedXml?.StandardChildrenAt(
                        GpxExtensionScope.RoutePoint,
                        routeIndex,
                        pointIndex),
                    cancellationToken: cancellationToken);
            }

            await writer.WriteEndElementAsync();
        }
    }

    private async Task WriteRouteHeaderAsync(Route route, int routeIndex)
    {
        IReadOnlyList<XElement>? standardChildren = preservedXml?.StandardChildrenAt(
            GpxExtensionScope.Route,
            routeIndex);
        if (standardChildren is null)
        {
            await GpxWriterFormatting.WriteOptionalElementAsync(writer, "name", route.Name);
        }
        else
        {
            await GpxPreservedContentWriter.WriteStandardChildrenWithTextReplacementsAsync(
                writer,
                standardChildren,
                cancellationToken,
                new Dictionary<string, string?> { ["name"] = route.Name });
        }

        if (preservedXml is null)
        {
            await GpxPreservedContentWriter.WriteExtensionXmlAsync(
                writer,
                route.UnsupportedExtensionXml,
                cancellationToken);
        }
        else
        {
            await GpxPreservedContentWriter.WritePreservedExtensionsAsync(
                writer,
                preservedXml.At(GpxExtensionScope.Route, routeIndex),
                cancellationToken);
        }
    }

    private async Task WriteTracksAsync()
    {
        for (int trackIndex = 0; trackIndex < document.Tracks.Count; trackIndex++)
        {
            Track track = document.Tracks[trackIndex];
            await writer.WriteStartElementAsync(null, "trk", GpxXml.NamespaceName);
            await WriteTrackHeaderAsync(track, trackIndex);
            await WriteTrackSegmentsAsync(track, trackIndex);
            await writer.WriteEndElementAsync();
        }
    }

    private async Task WriteTrackHeaderAsync(Track track, int trackIndex)
    {
        IReadOnlyList<XElement>? standardChildren = preservedXml?.StandardChildrenAt(
            GpxExtensionScope.Track,
            trackIndex);
        if (standardChildren is null)
        {
            await GpxWriterFormatting.WriteOptionalElementAsync(writer, "name", track.Name);
            await GpxWriterFormatting.WriteOptionalElementAsync(writer, "type", track.Type);
        }
        else
        {
            await GpxPreservedContentWriter.WriteStandardChildrenWithTextReplacementsAsync(
                writer,
                standardChildren,
                cancellationToken,
                new Dictionary<string, string?>
                {
                    ["name"] = track.Name,
                    ["type"] = track.Type
                });
        }

        await GpxPreservedContentWriter.WritePreservedExtensionsAsync(
            writer,
            preservedXml?.At(GpxExtensionScope.Track, trackIndex) ?? [],
            cancellationToken);
    }

    private async Task WriteTrackSegmentsAsync(Track track, int trackIndex)
    {
        for (int segmentIndex = 0; segmentIndex < track.Segments.Count; segmentIndex++)
        {
            TrackSegment segment = track.Segments[segmentIndex];
            await writer.WriteStartElementAsync(null, "trkseg", GpxXml.NamespaceName);
            for (int pointIndex = 0; pointIndex < segment.Points.Count; pointIndex++)
            {
                await GpxSchemaElementWriter.WritePointAsync(
                    writer,
                    "trkpt",
                    segment.Points[pointIndex],
                    null,
                    extensions: preservedXml?.At(
                        GpxExtensionScope.TrackPoint,
                        trackIndex,
                        segmentIndex,
                        pointIndex),
                    standardChildren: preservedXml?.StandardChildrenAt(
                        GpxExtensionScope.TrackPoint,
                        trackIndex,
                        segmentIndex,
                        pointIndex),
                    cancellationToken: cancellationToken);
            }

            await GpxPreservedContentWriter.WritePreservedExtensionsAsync(
                writer,
                preservedXml?.At(
                    GpxExtensionScope.TrackSegment,
                    trackIndex,
                    segmentIndex) ?? [],
                cancellationToken);
            await writer.WriteEndElementAsync();
        }
    }
}
