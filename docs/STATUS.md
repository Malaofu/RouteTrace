# Project status

**Phase:** Workspace development

**Current PBI:** PBI-072 — Multi-document workspace

**Last updated:** 2026-08-09

## Current state

- PBI-000 through PBI-072 are complete.
- Workspaces can display at least three GPX documents simultaneously with
  stable IDs and distinguishable default colours.
- Active, selected, and visible document state is independent and persists in
  versioned IndexedDB workspace records.
- Individual documents can be activated, selected, shown or hidden, closed,
  and exported without altering another document's canonical GPX data.
- Failed imports preserve the last valid multi-document workspace.
- GPX file commands and inspector visibility are available from the centered
  application menu and keyboard shortcuts.

## Verification

- Release build and WebAssembly AOT publish: passed with zero warnings.
- .NET tests: 57 passed, zero failed.
- Published Playwright tests: 10 passed, zero failed, including incremental
  selection with a full-density document loaded.
- Formatting and TypeScript checks: passed.

## Blockers

- None.

## Next action

- Select the next PBI explicitly; `CURRENT_PBI.md` remains PBI-072.

## Deferred choices

- Production map-data, routing, and map-matching providers.
- Image-processing implementation.
- Optional Aspire development telemetry in PBI-210.
- Azure deployment in PBI-220 and PBI-230.
