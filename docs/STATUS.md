# Project status

**Phase:** OpenLayers adapter decomposition complete

**Current PBI:** PBI-066 — Decompose the OpenLayers adapter

**Last updated:** 2026-08-16

## Current state

- PBI-063 through PBI-066 are complete; all engineering-review follow-ups are
  independently implemented and verified.
- `mapAdapter.ts` remains the single Blazor JavaScript module and delegates to
  focused lifecycle, geometry-synchronisation, and feature-styling modules.
- Small serialisable map contracts are isolated from OpenLayers imports; map
  projections and rendering-library types remain inside TypeScript.
- Endpoint, POI, selection, hover, and multi-document behaviour is unchanged.
- GPX and route folders retain their `Parsing`, `Preservation`, `Writing`,
  `Analysis`, `Documents`, `Geometry`, and `Workspaces` boundaries.
- FakeItEasy remains the agreed test-double framework.

## Verification

- TypeScript typecheck, asset build, and WebAssembly AOT publish: passed.
- .NET tests: 72 passed, zero failed.
- Published Playwright suite: 15 passed, zero failed.
- Full-density published import medians: 463.2 ms total and 0.8 ms busy
  feedback, within the 500 ms and 100 ms budgets; export completed in 74 ms.
- An initial Debug-host browser run passed 13 functional tests but missed two
  performance-only time limits; both passed against the published AOT output.

**Blockers:** None.

## Next action

- Select the next PBI explicitly; `CURRENT_PBI.md` remains PBI-066.
