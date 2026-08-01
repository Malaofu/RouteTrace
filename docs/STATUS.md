# Project status

**Phase:** Large GPX loading performance complete

**Current PBI:** PBI-051 — Loading performance

**Last updated:** 2026-08-01

## Progress

- PBI-000 through PBI-051 are complete.
- Large GPX imports copy browser files asynchronously in large chunks, then use
  a forward-only reader over memory instead of thousands of per-node async
  continuations or a complete XML DOM. Points are parsed directly without
  constructing thousands of temporary XML trees.
- The full-density 2.3 MB, 6,987-point fixture covers streaming import,
  extension preservation, and statistics semantics.
- Playwright measures the real file-selection-to-map-render path. Local Edge
  measurements improved from about 2,030 ms to roughly 1,300–1,420 ms;
  OpenLayers itself takes about 2 ms. CI enforces a two-second end-to-end
  budget.
- The accessible loading state now renders before parsing starts. Playwright
  measured feedback in under 1 ms and enforces a 100 ms feedback budget.
- Release publishing uses WebAssembly AOT. The published application imports
  and renders FX-GPX-002-a in about 182 ms locally, with parsing around 102 ms;
  CI enforces a 500 ms AOT budget.
- All 25 tests pass and the application builds with no warnings.

## Next action

Select the next PBI explicitly; none has been started automatically.

## Blockers

None.

## Manual verification

- Import FX-GPX-002-a and confirm the busy indicator clears promptly, the map
  renders the complete track, and the inspector reports 6,987 points and the
  Garmin TrackPointExtension namespace.
- Confirm the loading message and activity indicator appear immediately after
  choosing the file rather than only after parsing completes.

## Deferred choices

- Final product/solution name.
- Production map-data provider.
- Routing and map-matching provider.
- Image-processing implementation.
- Optional Aspire telemetry remains deferred to PBI-210.
