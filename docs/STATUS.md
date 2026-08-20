# Project status

**Phase:** Manual route editing

**Current PBI:** PBI-080 — Manual route editing

**Last updated:** 2026-08-19

## Current state

- PBI-080 is complete. Explorer context menus create the document → route →
  segment hierarchy and edit or delete routes and segments in place.
- Clicking or dragging a line inserts a point; dragging a point redraws the
  line live, including as the first action after entering edit mode, and
  right-clicking a point opens a Delete action.
- Editing shows start/finish markers; Ctrl+Z/Ctrl+Y share the reusable history,
  and Escape closes unchanged sessions or offers a theme-aware keep/discard
  dialog for changes.
- The background New document menu stays beside the pointer at the bottom of
  the explorer instead of reserving space for the full node action menu.
- Existing edits retain unaffected document structure, GPX extensions, and
  elevation/time metadata; changes remain canonical WGS 84 geometry and use
  normal workspace persistence and GPX export.
- Editing temporarily clears obstructing workspace chrome and focuses the
  selected line while keeping the document explorer available.

## Verification

- Formatting, strict TypeScript, warning-free build, and WebAssembly AOT
  publish: passed.
- Automated tests: 85 .NET and 25 published Playwright passed; zero failed.
- Published full-density import median: 494.4 ms total and 12.2 ms busy feedback;
  export completed in 119 ms.
- Debug/interpreter dense-GPX timing tests exceeded their performance budgets;
  the complete AOT-published suite, including all editing scenarios, passed.
- No additional manual verification steps are required.

**Blockers:** None.

## Next action

- Select the next PBI explicitly; do not start it implicitly.
