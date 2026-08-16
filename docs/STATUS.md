# Project status

**Phase:** Workspace development

**Current PBI:** PBI-075 — Route endpoints and POI symbols

**Last updated:** 2026-08-16

## Current state

- PBI-000 through PBI-075 are complete.
- Visible tracks and routes have derived start and finish badges; tracks use
  only their first and last populated points across all segments.
- Starts render as green circles and finishes as checkered-flag badges; loop
  endpoints are offset so both meanings remain legible at a shared coordinate.
- Waypoint `sym` aliases, marker assets, and sizing bind from `appsettings.json`
  through DI and typed options; map-pin layers, POI artwork, and the finish
  marker are standalone SVG assets.
  Missing and unknown values use a configured generic marker while original
  GPX symbols remain canonical and round-trip unchanged.
- Hovering a POI shows its name, symbol, coordinates, optional elevation, and
  description in a compact non-interactive card rendered and styled by the
  Blazor map feature; the OpenLayers adapter reports only hover identity and
  position.
- FX-GPX-004 contains common POI symbols and synthetic FX-GPX-006 covers open,
  loop, multi-segment, known-symbol, unknown-symbol, and missing-symbol cases.

## Verification

- Release build and WebAssembly AOT publish: passed with zero warnings.
- .NET tests: 65 passed, zero failed.
- Functional Playwright coverage and the marker-asset regression pass. The
  published large-file timing sample currently exceeds its 500 ms budget
  (565 ms; busy feedback 128 ms against 100 ms).
- Formatting and TypeScript checks: passed.

**Blockers:** Published large-file timing verification needs a clean passing sample.

## Next action

- Select the next PBI explicitly; `CURRENT_PBI.md` remains PBI-075.
