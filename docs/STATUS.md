# Project status

**Phase:** GPX codec decomposition complete

**Current PBI:** PBI-065 — Decompose GPX codec internals

**Last updated:** 2026-08-16

## Current state

- PBI-063 through PBI-065 are complete; the remaining engineering-review
  follow-ups remain independently selectable work.
- GPX import orchestration now delegates structural streaming state, element
  and value parsing, and extension namespace collection to focused units.
- GPX export separates document traversal, schema element writing, preserved
  content restoration, raw XML fragments, and numeric/XML formatting.
- The canonical route model, streaming parser, namespace ownership, element
  ordering, and unsupported-extension preservation semantics are unchanged.
- Core folders and namespaces mirror these boundaries: GPX internals use
  `Parsing`, `Preservation`, and `Writing`; route types use `Analysis`,
  `Documents`, `Geometry`, and `Workspaces`.
- FakeItEasy remains the agreed test-double framework.

## Verification

- Release build and WebAssembly AOT publish: passed with zero warnings.
- .NET tests: 72 passed, zero failed.
- Published Playwright suite: 15 passed, zero failed.
- All eight GPX fixtures passed structural round-trip equivalence checks;
  malformed input diagnostics and cancellation coverage passed.
- Full-density import medians: 478 ms total and 2.3 ms busy feedback, within
  the 500 ms and 100 ms release budgets; export completed in 71.4 ms.
- Formatting and TypeScript checks: passed.

**Blockers:** None.

## Next action

- Select the next PBI explicitly; `CURRENT_PBI.md` remains PBI-065.
