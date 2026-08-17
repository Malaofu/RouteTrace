# Project status

**Phase:** Document operation consolidation

**Current PBI:** PBI-068 — Consolidate document import and export orchestration

**Last updated:** 2026-08-17

## Current state

- PBI-068 is complete. Focused GPX import and export operations now own parsing,
  serialisation, filename selection, browser download, and result branching.
- `ApplicationMenu` retains menu/shortcut state, command availability, busy
  feedback, and notice presentation while delegating document operations.
- File-menu, Ctrl+S, and explorer exports execute through the same operation and
  filename rules. Picker and drop imports continue through one import path.
- Focused non-visual tests cover successful, invalid, and read-failure imports,
  the shared size limit, export filenames, and retained/omitted feedback.
- Coarse performance observations live in Playwright rather than document or UI
  orchestration; production code retains only functional browser interop.
- PBI-069 remains the outstanding semantic theme-token follow-up from the
  engineering quality audit.

## Verification

- Formatting, strict TypeScript, warning-free build, and WebAssembly AOT
  publish: passed.
- Automated tests: 77 .NET and 19 published Playwright passed; zero failed.
- Full-density import medians: 462.8 ms total and 13.3 ms busy feedback; export
  completed in 80 ms. The published import hard ceiling is 1,000 ms; the
  separate busy-feedback ceiling remains 100 ms.

**Blockers:** None.

## Next action

- Select the next PBI explicitly; do not start it implicitly.
