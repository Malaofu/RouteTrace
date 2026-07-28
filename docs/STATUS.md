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

## Next

- Keep `CURRENT_PBI.md` at PBI-000 until the user explicitly selects the next
  PBI.
- Replace placeholder tests when custom behavior is introduced by a later PBI.

## Blockers

None.

## Deferred choices

- Final product/solution name.
- Map library and map-data provider.
- Routing and map-matching provider.
- Image-processing implementation.
- Timing and compatible package version for optional Aspire telemetry
  (PBI-210).

## Handoff note

PBI-000 is complete. On 2026-07-28 the solution formatted and built with zero
warnings, both tests passed, the Web project published successfully, and a
plain local static server returned HTTP 200 for both `index.html` and the
fingerprinted Blazor framework script. Do not start PBI-010 until the user
explicitly advances `CURRENT_PBI.md`.
