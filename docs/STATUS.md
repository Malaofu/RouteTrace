# Project status

**Phase:** GPX export awaiting independent-viewer verification

**Current PBI:** PBI-060 — GPX export and round trip

**Last updated:** 2026-08-01

## Progress

- PBI-000 through PBI-051 are complete. PBI-060 implementation is complete but
  its independent-viewer acceptance check remains open.
- The core exporter writes ordered GPX 1.1 with a required creator and retains
  metadata including author, annotated and linked waypoints, routes, tracks,
  segment boundaries, elevation, and time.
- Metadata and route extensions retain their original ownership. Other opaque
  extensions retain a lazy owner-path index so track-point extensions return to
  their original containers without slowing initial import.
- Track type is retained. Latitude and longitude are exported consistently with
  exactly seven fractional digits.
- Imported root namespace declarations are restored, including Garmin's
  `xmlns:gpxtpx` declaration used by TrackPointExtension elements; redundant
  per-extension declarations are removed.
- Exported elevation always includes a decimal point and at least one
  fractional digit.
- The Web application downloads the current document locally as
  `route-trace.gpx`.
- Round-trip tests cover all required GPX fixtures and validate output offline
  against the official Topografix GPX 1.1 schema.
- File-level round-trip comparison covers every committed GPX fixture. Only the
  creator and numerically equivalent decimal formatting may differ.
- FX-GPX-005 synthetically populates every GPX 1.1 standard field and extension
  scope defined by the official XSD. Uninterpreted standard fields use lazy
  owner-scoped pass-through so their values and ordering remain intact.
- Preserved standard fields and extensions share one lazy owner-scoped snapshot;
  export parses it once and writes retained XML elements directly.
- Browser tests measure full-density export. Local AOT export completes in about
  170 ms versus roughly 2.1–3.4 seconds interpreted; budgets are 500 ms AOT and
  5 seconds for interpreted development runs.
- All 41 .NET tests pass and the application builds with no warnings. Three
  Playwright performance tests cover import, export, and parser profiling.

## Next action

Open a downloaded file in an independent GPX viewer and record the result.

## Blockers

- Independent GPX viewer verification has not yet been performed in this
  workspace.

## Manual verification

- Import FX-GPX-003, choose **Download GPX**, and confirm the browser downloads
  `route-trace.gpx`.
- Open that file in an independent GPX viewer and confirm both tracks render and
  the gap between the first track's two segments is not joined.

## Deferred choices

- Final product/solution name.
- Production map-data provider.
- Routing and map-matching provider.
- Image-processing implementation.
- Optional Aspire telemetry remains deferred to PBI-210.
