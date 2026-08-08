# Repository instructions

## Required context

At the start of an implementation task, read only:

1. `docs/CURRENT_PBI.md`
2. `docs/WORKFLOW.md`
3. `docs/STATUS.md`
4. The single matching PBI section in `docs/BACKLOG.md`
5. The resolved PBI's entry in `docs/FIXTURES.md`
6. The portions of `docs/ARCHITECTURE.md` explicitly required by the resolved
   PBI

`CURRENT_PBI.md` contains exactly one PBI identifier such as `PBI-030`. Resolve
that identifier to one `## PBI-NNN` section in `BACKLOG.md`. Stop if the value
is invalid, missing, or matches anything other than one section.

Do not read the complete backlog, roadmap, or decision history. Read only the
resolved PBI section unless the user asks for planning, reprioritisation, or
architectural review.

## Scope control

- Implement only the PBI resolved from `docs/CURRENT_PBI.md`.
- Do not begin later PBIs, even when their implementation appears convenient.
- Add only plumbing required for the current acceptance criteria.
- Prefer a narrow vertical slice over speculative abstractions.
- Do not add authentication, a database, a production server API, cloud
  persistence, collaboration, telemetry, or deployment infrastructure unless
  the current PBI explicitly requires it.
- PBI-210 Aspire and PBIs 220–230 Azure deployment are independent later work;
  do not introduce them as incidental plumbing.
- When a missing decision materially affects the current PBI, stop and present
  the smallest set of concrete options.
- Treat ideas discovered during implementation as backlog candidates, not as
  permission to implement them.

## Architecture rules

- The initial application is a standalone .NET 10 Blazor WebAssembly
  application suitable for static hosting.
- Domain and GPX logic must not depend on browser UI, map libraries, or a
  particular routing provider.
- Geographic coordinates in the domain model use WGS 84 latitude/longitude.
  Map projections are adapter concerns.
- Browser, map, image-processing, storage, elevation, and routing integrations
  must remain behind narrow boundaries.
- Imported images remain local to the browser unless a future PBI explicitly
  changes that policy.
- Preserve unsupported GPX extension XML when practical; do not silently
  reinterpret vendor extensions.
- Aspire AppHost and the Blazor Gateway are development-time orchestration and
  telemetry infrastructure. They must not become a production runtime
  dependency for the statically published Web project.
- Release publishing uses WebAssembly AOT. Do not disable AOT to repair a build
  environment; install the required `wasm-tools` workload instead.
- A workspace may contain multiple canonical route documents. Keep active,
  selected, and visible state distinct.
- Colour, visibility, explorer state, and derived endpoint markers are
  presentation data. Do not write them into GPX vendor extensions.

## Fixture gate

Before planning or implementation:

1. Read the resolved PBI's fixture entry in `docs/FIXTURES.md`.
2. Give the user a short, explicit fixture report:
   - `No fixtures needed for PBI-NNN.`
   - `All fixtures for PBI-NNN are present: ...`
   - `Fixtures needed for PBI-NNN: ...`
3. For each missing fixture, state its ID, purpose, required characteristics,
   accepted file types, whether a synthetic fixture is acceptable, and any
   sanitisation needed.
4. Pause fixture-dependent work until the user supplies the fixture or
   explicitly approves a synthetic/public substitute. Unrelated work may
   continue only when it cannot prejudice the fixture-dependent design.

Do not silently replace real exporter/device examples with invented data.
Never commit personal location history, account identifiers, access tokens, or
other secrets in a fixture.

## Implementation expectations

- Use modern .NET; do not target .NET Framework.
- Keep warnings enabled and nullable reference types enabled.
- Add focused automated tests for non-visual logic introduced by the PBI.
- Avoid adding packages until their immediate use is demonstrated.
- Add to `docs/DECISIONS.md` only when the PBI makes a durable cross-cutting
  product or architecture choice.
- Keep progress updates concise: report material findings, blockers, and
  verification rather than narrating routine commands.

## Documentation maintenance

- Documentation describes current state; Git history records how it changed.
- Rewrite `docs/STATUS.md` as a snapshot instead of appending a chronology.
- Keep `docs/STATUS.md` at approximately 40 lines or fewer.
- Report verification totals and failures, not raw command output or repeated
  benchmark samples.
- Update the active backlog entry with final status and material scope changes,
  not an implementation diary.
- Keep decisions to the decision, reason, and important consequences. Routine
  code structure, tuning steps, and individual measurements are not decisions.
- Do not repeat the same completion details across status, architecture,
  decisions, and backlog.
- Follow the detailed file responsibilities in `docs/WORKFLOW.md`.

## Completion

A PBI is complete only when:

- All acceptance criteria in the resolved `docs/BACKLOG.md` section are
  satisfied.
- Relevant tests pass.
- The application builds.
- Any manual verification steps are recorded.
- `docs/STATUS.md` reflects the result and remaining blockers.
- The active backlog entry is marked complete without expanding later PBIs.

Leave `docs/CURRENT_PBI.md` unchanged. Do not automatically select or start the
next PBI.

## Command execution

- Run ordinary read, search, build, and test commands without requesting
  confirmation when permitted by the active sandbox.
- Prefer direct commands over explicit `pwsh -Command` wrappers when possible.
- Keep commands separate unless combining them is necessary.
- Use `rg` or `rg --files` for repository searches.
- Use `Get-Content` only for simple file reads where `rg` is unsuitable.
- Do not request elevated execution merely because an ordinary command failed;
  first determine whether the failure is caused by its arguments, working
  directory, or sandbox access.
