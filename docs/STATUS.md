# Project status

**Phase:** Razor component structure review complete

**Current PBI:** PBI-064 — Extract substantial Razor component logic

**Last updated:** 2026-08-16

## Current state

- PBI-064 is complete; the other engineering-review follow-ups remain
  independently selectable work.
- Accepted architecture decisions are compliant or not yet applicable; none
  are superseded and no unresolved decision drift remains.
- `Home`, `ApplicationMenu`, `DocumentExplorer`, `MapViewport`, and
  `WorkspacePanel` keep their markup and directives in `.razor` files while
  their substantial behaviour resides in matching feature-scoped partial
  classes.
- Small presentation-only components remain inline, and no generic component
  base abstraction was introduced.
- Dependency injection, component parameters, and async disposal contracts are
  unchanged. FakeItEasy remains the agreed test-double framework.

## Verification

- Release build and WebAssembly AOT publish: passed with zero warnings.
- .NET tests: 69 passed, zero failed.
- Published Playwright suite: 15 passed, zero failed.
- Full-density import medians: 473.5 ms total and 2.8 ms busy feedback, within
  the 500 ms and 100 ms release budgets; export completed in 71.8 ms.
- Formatting and TypeScript checks: passed.
- Structural review confirmed the five listed Razor files contain no `@code`
  blocks and retain their existing rendered markup.

**Blockers:** None.

## Next action

- Select the next PBI explicitly; `CURRENT_PBI.md` remains PBI-064.
