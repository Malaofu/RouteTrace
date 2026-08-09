# Project status

**Phase:** Workspace development

**Current PBI:** PBI-073 — GPX document explorer

**Last updated:** 2026-08-09

## Current state

- PBI-000 through PBI-073 are complete.
- Workspaces can display at least three GPX documents simultaneously with
  stable IDs and distinguishable default colours.
- Active, selected, and visible document state is independent and persists in
  versioned IndexedDB workspace records.
- The desktop explorer sits beside the map and returns its width when hidden;
  narrow layouts use an overlay drawer.
- It shows document, track, segment, route, and waypoint hierarchy without
  creating nodes for individual track points.
- Clicking a semantic row activates and identifies it on the map without
  changing expansion state; group selection includes its complete subtree.
- The explorer and inspector can be toggled independently from View or keyboard
  shortcuts without changing workspace state.
- Workspace controls now focus on naming, automatic persistence, and saved
  workspace management rather than duplicating explorer actions.

## Verification

- Release build and WebAssembly AOT publish: passed with zero warnings.
- .NET tests: 57 passed, zero failed.
- Published Playwright tests: 11 passed, zero failed, including semantic
  hierarchy, explorer state, and full-density incremental selection coverage.
- Formatting and TypeScript checks: passed.

## Blockers

- None.

## Next action

- Select the next PBI explicitly; `CURRENT_PBI.md` remains PBI-073.

## Deferred choices

- Production map-data, routing, and map-matching providers.
- Image-processing implementation.
- Optional Aspire development telemetry in PBI-210.
- Azure deployment in PBI-220 and PBI-230.
