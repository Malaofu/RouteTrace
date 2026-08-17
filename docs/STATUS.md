# Project status

**Phase:** Semantic theme coverage

**Current PBI:** PBI-069 — Complete semantic theme coverage

**Last updated:** 2026-08-17

## Current state

- PBI-069 is complete. The application menu, explorer, semantic tree states,
  context actions, fields, and dialogs now use the global theme properties.
- Light, dark, and Auto themes apply consistently to major application chrome;
  explicit choices survive reloads and Auto follows live system changes.
- Hover, active, disabled, focus, border, text, and muted states derive from
  shared semantic surface, text, border, hover, and accent properties.
- Map overlays retain explicit dark contrast colours because they render above
  variable map imagery; the neutral modal backdrop remains presentation-only.
- Published browser coverage compares representative computed backgrounds,
  borders, and text colours while preserving keyboard interaction coverage.

## Verification

- Formatting, strict TypeScript, warning-free build, and WebAssembly AOT
  publish: passed.
- Automated tests: 77 .NET and 20 published Playwright passed; zero failed.
- Full-density import medians: 472.7 ms total and 14.2 ms busy feedback; export
  completed in 89 ms. The published import hard ceiling is 1,000 ms; the
  separate busy-feedback ceiling remains 100 ms.

**Blockers:** None.

## Next action

- Select the next PBI explicitly; do not start it implicitly.
