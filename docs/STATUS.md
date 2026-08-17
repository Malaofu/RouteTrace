# Project status

**Phase:** Import interaction restoration

**Current PBI:** PBI-067 — Restore drag-and-drop GPX import

**Last updated:** 2026-08-17

## Current state

- PBI-067 is complete. Dragging a file over the application presents a clear
  drop target and dropping it starts the established picker import workflow.
- Picker and drop imports share the same browser file input, size limit, GPX
  parser, performance measurements, feedback, and workspace-addition path.
- The browser adapter only translates file drag events into the existing input
  change event; GPX content remains browser-local.
- Browser coverage verifies matching picker/drop results and confirms that an
  invalid drop preserves the existing workspace and rendered map document.
- PBI-068 remains the next import/export orchestration cleanup. PBI-069 remains
  the outstanding semantic theme-token follow-up from the quality audit.

## Verification

- Formatting, strict TypeScript, warning-free build, and WebAssembly AOT
  publish: passed.
- Automated tests: 72 .NET and 18 published Playwright passed; zero failed.
- Full-density import medians: 493.6 ms total and 3.1 ms busy feedback; export
  completed in 72.5 ms. The published import hard ceiling is 1,000 ms; the
  separate busy-feedback ceiling remains 100 ms.

**Blockers:** None.

## Next action

- Select the next PBI explicitly; do not start it implicitly.
