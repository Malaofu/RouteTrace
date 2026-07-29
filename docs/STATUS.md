# Project status

**Phase:** Interactive map foundation

**Current PBI:** PBI-010 — Interactive map shell

**Last updated:** 2026-07-29

## Completed

- Initial product scope defined.
- Static-first architecture direction defined.
- Ordered roadmap and initial PBI backlog created.
- Codex scope-control workflow defined.
- .NET 10 solution with Core, Web, Core.Tests, and Web.Tests projects created.
- Nullable references and central package management enabled.
- Debug build, formatting verification, and Release Web publish succeed.
- Aspire development orchestration and telemetry deferred to PBI-210.
- Test runner configuration aligned with the existing xUnit/VSTest projects;
  both placeholder tests pass.
- Application-level error boundary added.
- Warnings are treated as errors for all projects.
- GitHub Actions CI added for formatting, build, test, and publish validation on
  `main` pushes and pull requests.
- Build, test, publish, and static-serving commands documented in the README.
- MIT license added.
- The frequently used source projects remain at the solution root; solution
  folders expose tests, documentation, CI, and repository-level files in Rider
  and Visual Studio.
- PBI-000 acceptance criteria completed.
- OpenLayers 10.10.0 selected behind a strict TypeScript adapter.
- Responsive map shell supports pan, zoom, resize observation, disposal, and
  WGS 84 fit-to-bounds.
- Standard OpenStreetMap raster tiles selected for light development use with
  visible attribution and documented provider constraints.
- esbuild and TypeScript integrated into local builds and CI.
- PBI-010 acceptance criteria completed.

## Next

- Keep `CURRENT_PBI.md` at PBI-010 until the user explicitly selects the next
  PBI.
- Repeat the documented browser interaction checks after future map-shell
  changes.

## Blockers

None.

## Deferred choices

- Final product/solution name.
- Production map-data provider.
- Routing and map-matching provider.
- Image-processing implementation.
- Timing and compatible package version for optional Aspire telemetry
  (PBI-210).

## Handoff note

PBI-010 is complete. On 2026-07-29 the strict TypeScript build, formatting,
warnings-as-errors Release build, both .NET tests, and static publish passed. A
headless browser rendered the map, zoom controls, fitted initial view, and
visible OpenStreetMap attribution at both 1440×900 and 600×800. Manual pan,
zoom, resize, and attribution verification steps are recorded in the README.
Do not start PBI-020 until the user explicitly advances `CURRENT_PBI.md`.
