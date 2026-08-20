# Project status

**Phase:** Manual route editing

**Current PBI:** PBI-090 — Bicycle routing between anchors

**Last updated:** 2026-08-20

## Current state

- PBI-090 is complete. Routes and track segments distinguish persisted editing
  anchors from replaceable, non-interactive geometry points.
- GPX import heuristically promotes endpoints and significant direction changes
  to anchors; GPX export remains standard point geometry without app metadata.
- Adding, inserting, moving, or deleting an anchor recalculates only affected
  legs through the configurable BRouter adapter. The editor offers Cycling,
  Gravel, and MTB modes and remembers the latest browser-local selection.
- Changing routing mode recalculates every anchor-to-anchor leg. Direction
  arrows are screen-spaced and capped so dense geometry does not hide anchors.
- Routing progress, no-route, and provider-failure states are visible. A failed
  multi-leg edit does not replace the last valid geometry.
- Workspace schema 4 preserves anchor indices outside GPX and reads schemas
  1–3 with heuristic anchor migration.
- The editor discloses that edited anchor coordinates are sent to brouter.de;
  no browser credential is required.

## Verification

- Formatting, strict TypeScript, warning-free build, and WebAssembly AOT
  publish: passed.
- Automated tests: 94 .NET and 28 AOT-published Playwright passed; zero failed.
- Full-density import/export performance scenarios passed in the published app.
- Live BRouter checks returned bicycle GeoJSON for `fastbike`, `gravel`, and
  `mtb` using synthetic Copenhagen coordinates.

**Blockers:** None.

## Next action

- Select the next PBI explicitly; do not start it implicitly.
