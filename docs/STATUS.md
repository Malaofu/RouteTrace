# Project status

**Phase:** Workspace development

**Current PBI:** PBI-071 — Application menu and command surface

**Last updated:** 2026-08-09

## Current state

- PBI-000 through PBI-071 are complete.
- GPX import and download are available from a compact File menu and through
  Ctrl/Cmd+O and Ctrl/Cmd+S keyboard shortcuts.
- Menu items and shortcuts share command availability; unavailable downloads
  are disabled and cannot execute.
- The File menu supports keyboard focus, Escape dismissal, outside-click
  dismissal, and leaves the map interaction surface unobstructed.
- Workspaces contain multiple canonical route documents and persist
  automatically in versioned IndexedDB records.

## Verification

- Release build and WebAssembly AOT publish: passed with zero warnings.
- .NET tests: 55 passed, zero failed.
- Published Playwright tests: 6 passed, zero failed.
- Formatting and TypeScript checks: passed.

## Blockers

- None.

## Next action

- Select the next PBI explicitly; `CURRENT_PBI.md` remains PBI-071.

## Deferred choices

- Production map-data, routing, and map-matching providers.
- Image-processing implementation.
- Optional Aspire development telemetry in PBI-210.
- Azure deployment in PBI-220 and PBI-230.
