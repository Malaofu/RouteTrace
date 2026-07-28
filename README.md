# Route Trace

Working project notes for a browser-based tool that converts route images into
portable cycling-route files.

`Route Trace` is a working name only. Renaming the solution does not require an
architecture decision.

## Working with Codex

Codex should begin with:

1. [`AGENTS.md`](AGENTS.md)
2. [`docs/STATUS.md`](docs/STATUS.md)
3. [`docs/CURRENT_PBI.md`](docs/CURRENT_PBI.md)

Those files define the current scope. Codex should only open the broader
project documents when the current PBI requires them.

## Project documents

- [`docs/PROJECT.md`](docs/PROJECT.md): product scope and principles.
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md): intended solution structure
  and technical boundaries.
- [`docs/ROADMAP.md`](docs/ROADMAP.md): delivery phases and ordering.
- [`docs/BACKLOG.md`](docs/BACKLOG.md): candidate PBIs and acceptance criteria.
- [`docs/CURRENT_PBI.md`](docs/CURRENT_PBI.md): the implementation-scope
  identifier for the current work session.
- [`docs/FIXTURES.md`](docs/FIXTURES.md): test-data inventory and the mandatory
  fixture check before implementation.
- [`docs/STATUS.md`](docs/STATUS.md): concise record of current progress.
- [`docs/DECISIONS.md`](docs/DECISIONS.md): durable technical and product
  decisions.

## Weekend workflow

1. Select one PBI from `docs/BACKLOG.md`.
2. Put only its identifier, for example `PBI-030`, in
   `docs/CURRENT_PBI.md`.
3. Ask Codex to plan and implement only that PBI.
4. Codex reports any required fixtures before beginning fixture-dependent
   work.
5. Provide the requested fixtures, approve a synthetic substitute, or defer
   the affected work.
6. Verify the acceptance criteria.
7. Update `docs/STATUS.md` and mark the PBI complete in `docs/BACKLOG.md`.
8. Select another PBI only in a later, explicit step.
