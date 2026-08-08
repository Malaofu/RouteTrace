# Project status

**Phase:** Workspace development

**Current PBI:** PBI-070 — Browser-local workspace persistence

**Last updated:** 2026-08-08

## Current state

- PBI-000 through PBI-070 are complete.
- GPX 1.1 import, inspection, map visualisation, export, schema validation, and
  semantic round-trip coverage are implemented.
- Workspaces contain multiple canonical route documents with stable IDs and a
  distinct active-document ID.
- Workspace changes are stored automatically in versioned IndexedDB records;
  startup restores the most recently active workspace without user action.
- Saved workspaces can still be listed, renamed, reopened, and deliberately
  deleted without accounts or cloud storage.
- Corrupt and incompatible saved data is reported without breaking the app.

## Verification

- Release build and AOT publish: passed with zero warnings.
- .NET tests: 53 passed, zero failed.
- Playwright tests: 4 passed, zero failed, including persistence across reload.
- Formatting and TypeScript checks: passed.

## Blockers

- None.

## Next action

- Select the next PBI explicitly; `CURRENT_PBI.md` remains PBI-070.

## Deferred choices

- Production map-data, routing, and map-matching providers.
- Image-processing implementation.
- Optional Aspire development telemetry in PBI-210.
- Azure deployment in PBI-220 and PBI-230.
