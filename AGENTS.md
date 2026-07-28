# Repository instructions

## Required context

At the start of an implementation task, read only:

1. `docs/STATUS.md`
2. `docs/CURRENT_PBI.md`
3. The single matching PBI section in `docs/BACKLOG.md`
4. `docs/FIXTURES.md`
5. The portions of `docs/ARCHITECTURE.md` explicitly referenced by the resolved
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

## Fixture gate

Before planning or implementation:

1. Read the resolved PBI's fixture entry in `docs/FIXTURES.md`.
2. Give the user a short, explicit fixture report:
   - no fixtures are needed;
   - all required fixtures are already present; or
   - fixtures must be supplied.
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
- Record a durable architectural choice in `docs/DECISIONS.md`.
- Keep `docs/STATUS.md` short. It is a hand-off, not a work diary.

## Completion

A PBI is complete only when:

- All acceptance criteria in the resolved `docs/BACKLOG.md` section are
  satisfied.
- Relevant tests pass.
- The application builds.
- Any manual verification steps are recorded.
- `docs/STATUS.md` reflects the result and remaining blockers.

Do not automatically select or start the next PBI.
