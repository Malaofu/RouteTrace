# Project status

**Phase:** GPX export and round trip complete

**Current PBI:** PBI-060 — GPX export and round trip

**Last updated:** 2026-08-01

## Progress

- PBI-000 through PBI-060 are complete.
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
- The inspector shows mapped author/contact details, track types, route names,
  and waypoint annotations and links in addition to statistics.
- Downloads use the GPX metadata name when available, otherwise the imported
  filename, with `route-trace.gpx` as the final fallback.
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
- All 47 .NET tests pass and the application builds with no warnings. Three
  Playwright performance tests cover import, export, and parser profiling.

## Next action

Select the next PBI explicitly; none has been started automatically.

## Blockers

None.

## Manual verification

- The user reviewed input/output differences across the supplied GPX examples
  and explicitly accepted PBI-060 completion.
- Browser automation confirms the full-density document downloads with its
  imported filename and remains within the export performance budget.
- No named independent-viewer run was performed in this workspace. Product
  sign-off accepts the official GPX 1.1 schema validation and complete
  file-level round-trip comparison as sufficient compatibility evidence for
  this PBI.

## Deferred choices

- Final product/solution name.
- Production map-data provider.
- Routing and map-matching provider.
- Image-processing implementation.
- Optional Aspire telemetry remains deferred to PBI-210.
