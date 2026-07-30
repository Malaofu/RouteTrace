# Project status

**Phase:** GPX import and validation

**Current PBI:** PBI-030 — GPX import and validation

**Last updated:** 2026-07-30

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
- Bootstrap and the default Counter/Weather samples removed.
- Structured global and component-level SCSS, design tokens, and responsive
  application chrome added; generated component CSS feeds Blazor isolation.
- Light, Dark, and system-following Auto themes added with a persisted browser
  preference and pre-Blazor theme application through external scripts.
- Provider- and UI-independent WGS 84 route primitives added to Core.
- Route documents support tracks, explicit segment discontinuities, routes,
  waypoints, optional elevation and time, and derived geographic bounds.
- Invalid coordinates and null collection entries are rejected; model
  collections are defensively copied.
- Core tests cover empty documents, multiple tracks and segments, aggregate
  bounds, invalid coordinates, and optional point data.
- Test projects consistently use xUnit.net v3, Shouldly, FakeItEasy, and
  Coverlet with centrally managed versions.
- GitHub CI presents TRX test results and combined coverage in checks and job
  summaries, with detailed reports retained as a workflow artifact.
- GitHub-maintained workflow actions use Node.js 24-compatible major versions.
- PBI-020 acceptance criteria completed.
- All four PBI-030 GPX fixtures are prepared and documented, including a
  sanitised real Strava/Wahoo export and a supplemented RideWithGPS export.
- Test projects and fixture data are consistently located under the
  architecture-defined `tests/` directory.

## Next

- Implement GPX 1.1 parsing and validation against the prepared fixtures.
- Add browser-local file selection and drag-and-drop over the parser boundary.

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

PBI-030 fixture preparation is complete. The raw user-supplied GPX and FIT
files remain only in ignored `tmp/`; committed candidates are sanitised,
synthetic, or explicitly supplemented and documented. Parser and browser UI
implementation have not started.
