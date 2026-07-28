# Project status

**Phase:** Foundation implementation

**Current PBI:** PBI-000 — Repository and static application baseline

**Last updated:** 2026-07-28

## Completed

- Initial product scope defined.
- Static-first architecture direction defined.
- Ordered roadmap and initial PBI backlog created.
- Codex scope-control workflow defined.
- .NET 10 solution with Core, Web, Core.Tests, and Web.Tests projects created.
- Nullable references and central package management enabled.
- Debug build, formatting verification, and Release Web publish succeed.
- Aspire development orchestration and telemetry deferred to PBI-210.

## Next

- Align the test projects with the Microsoft.Testing.Platform runner selected
  in `global.json`, or remove that runner selection.
- Replace placeholder tests with focused baseline tests where useful.
- Add the application error boundary.
- Document build, test, publish, and static-server smoke-check commands.
- Add CI for build and test.
- Serve the published output from a plain static server and record the manual
  verification.

## Blockers

- `dotnet test RouteTrace.slnx --no-build` fails because both test projects use
  VSTest while `global.json` selects Microsoft.Testing.Platform.

## Deferred choices

- Final product/solution name.
- Map library and map-data provider.
- Routing and map-matching provider.
- Image-processing implementation.
- Timing and compatible package version for optional Aspire telemetry
  (PBI-210).

## Handoff note

PBI-000 is in progress. Do not start PBI-010 until the remaining baseline
acceptance criteria are complete and the user explicitly advances
`CURRENT_PBI.md`.
