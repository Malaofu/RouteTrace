# Project status

**Phase:** Document explorer decomposition complete

**Current PBI:** PBI-063 — Decompose document explorer responsibilities

**Last updated:** 2026-08-16

## Current state

- PBI-063 and PBI-064 are complete; the other engineering-review follow-ups
  remain independently selectable work.
- Accepted architecture decisions are compliant or not yet applicable; none
  are superseded and no unresolved decision drift remains.
- `DocumentExplorer` now coordinates selection only. `DocumentTree` owns
  semantic rendering and expansion, while `DocumentExplorerActions` owns
  context-menu positioning, dialogs, and workspace action orchestration.
- Tree and action styles follow their rendering components, preserving pointer
  positioning, keyboard accessibility, visibility, and appearance behavior.
- Expansion state and semantic target construction have focused unit coverage;
  the tree still stops at segments and waypoints rather than track points.
- Existing workspace command paths and FakeItEasy remain unchanged.

## Verification

- Release build and WebAssembly AOT publish: passed with zero warnings.
- .NET tests: 72 passed, zero failed.
- Published Playwright suite: 15 passed, zero failed.
- Full-density import medians: 475.4 ms total and 0.5 ms busy feedback, within
  the 500 ms and 100 ms release budgets; export completed in 69.8 ms.
- Formatting and TypeScript checks: passed.

**Blockers:** None.

## Next action

- Select the next PBI explicitly; `CURRENT_PBI.md` remains PBI-063.
