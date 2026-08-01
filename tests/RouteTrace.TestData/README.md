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

## FX-GPX-002-a-strava-wahoo-full-density-sanitised.gpx

- **Fixture ID:** FX-GPX-002-a
- **Provenance:** Derived from the same user-supplied Wahoo ELEMNT Bolt v2
  activity and Strava GPX export as FX-GPX-002.
- **Sanitisation:** The same private start and end portions were removed by
  retaining source points 299 through 7285 inclusive. The track name was
  replaced, and all timestamps were shifted so the first retained point is
  `2020-01-01T09:00:00Z`.
- **Retained intentionally:** All 6,987 interior points without sampling,
  coordinates outside the private endpoint areas, elevation, relative timestamp
  intervals, and Garmin TrackPointExtension values.
- **Not committed:** The original full GPX and Wahoo FIT files.
- **Expected content:** One full-density GPX 1.1 track and segment for import
  performance validation.

## FX-GPX-003-multiple-tracks-segments.gpx

- **Fixture ID:** FX-GPX-003
- **Provenance:** Synthetic, created for Route Trace.
- **Expected content:** Two tracks and three segments. The two segments in the
  first track are geographically separated to represent a deliberate gap.

## FX-GPX-004-gpx-studio-supplemented.gpx

- **Fixture ID:** FX-GPX-004
- **Provenance:** A user-supplied purpose-created GPX exported from gpx.studio.
- **Unmodified exporter content:** GPX creator and metadata, 39-point track,
  elevations, and four user-created waypoints with names and optional details.
- **Synthetic supplement:** A five-point route derived from the supplied track
  and two Route Trace fixture-extension elements provide parser coverage that
  gpx.studio does not export.
- **Sanitisation:** No personal identifiers, timestamps, or private movement
  history were present. The source describes a purpose-created test route.
- **Expected content:** Metadata, track and route points, four waypoints,
  elevation, and unsupported extension XML in a non-GPX namespace.

## FX-ELE-001-elevation-coverage.gpx

- **Fixture ID:** FX-ELE-001
- **Provenance:** Synthetic, created for Route Trace with user approval.
- **Expected content:** Three equivalent short track samples with respectively
  complete, partial, and absent elevation. The complete sample has elevations
  `10, 15, 12, 20` metres for deterministic range and ascent/descent checks.
- **Sanitisation:** Coordinates are synthetic points near latitude/longitude
  zero and contain no movement history or identifiers.
