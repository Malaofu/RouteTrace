# GPX test fixtures

These files support PBI-030 parser tests. They contain no account identifiers,
tokens, or private start/end locations.

## FX-GPX-001-minimal-track.gpx

- **Fixture ID:** FX-GPX-001
- **Provenance:** Synthetic, created for Route Trace.
- **Expected content:** GPX 1.1 metadata and one track segment containing three
  points with elevation and UTC timestamps.

## FX-GPX-002-strava-wahoo-sanitised.gpx

- **Fixture ID:** FX-GPX-002
- **Provenance:** A user-supplied activity recorded by a Wahoo ELEMNT Bolt v2
  and exported from Strava as GPX.
- **Sanitisation:** The first and last 2 km were removed, the remaining activity
  was evenly sampled to 196 points, the track name was replaced, and all
  timestamps were shifted so the first retained point is
  `2020-01-01T09:00:00Z`.
- **Retained intentionally:** Coordinates outside the stated private areas,
  elevation, relative timestamp intervals, and Garmin TrackPointExtension
  heart-rate and temperature values.
- **Not committed:** The original full GPX and Wahoo FIT files.
- **Expected content:** One GPX 1.1 track and segment with real exporter
  structure and unsupported Garmin extension XML.

## FX-GPX-003-multiple-tracks-segments.gpx

- **Fixture ID:** FX-GPX-003
- **Provenance:** Synthetic, created for Route Trace.
- **Expected content:** Two tracks and three segments. The two segments in the
  first track are geographically separated to represent a deliberate gap.

## FX-GPX-004-ridewithgps-supplemented.gpx

- **Fixture ID:** FX-GPX-004
- **Provenance:** Based on a short user-supplied public-road GPX Track exported
  from the free RideWithGPS service.
- **Unmodified exporter content:** GPX creator, track geometry, elevations, and
  document name.
- **Sanitisation:** The public RideWithGPS route URL was replaced with a
  reserved `example.test` URL while preserving the metadata-link structure.
- **Synthetic supplement:** Three waypoints named `Golf Course`, `Crosswalk`,
  and `Tree`; a five-point GPX route; Route Trace fixture-extension elements;
  and a deterministic metadata timestamp.
- **Expected content:** Metadata, a real RideWithGPS track, route points,
  waypoints, elevation, and unsupported extension XML in a non-GPX namespace.
