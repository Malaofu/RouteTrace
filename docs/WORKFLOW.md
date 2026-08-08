# Codex workflow

This document defines how Codex uses and maintains the project notes. The
notes describe current state and agreed scope; Git history records how they
changed.

## Start of a task

1. Read `CURRENT_PBI.md` and treat its single identifier as the complete work
   boundary.
2. Read only the matching section of `BACKLOG.md`.
3. Read `STATUS.md` and check `FIXTURES.md` before planning implementation.
4. Open `PROJECT.md`, `ARCHITECTURE.md`, or `DECISIONS.md` only when the active
   PBI requires broader context.
5. Report the fixture result using the exact wording required by
   `FIXTURES.md` before fixture-dependent work begins.

Do not implement, prepare, or select a later PBI without an explicit user
request.

## Document responsibilities

- `PROJECT.md`: stable product scope, principles, and non-goals.
- `ARCHITECTURE.md`: current technical structure and boundaries.
- `ROADMAP.md`: phase ordering and release markers.
- `BACKLOG.md`: PBI goals, tasks, acceptance criteria, and current status.
- `CURRENT_PBI.md`: exactly one `PBI-NNN` identifier.
- `FIXTURES.md`: fixture inventory and acquisition requirements.
- `STATUS.md`: short snapshot of current state, blockers, verification, and
  next action.
- `DECISIONS.md`: durable cross-cutting decisions, not implementation notes.

## Updating documentation

- Rewrite snapshot sections instead of appending a chronological log.
- Use Git history, commits, and pull requests for implementation history.
- Keep `STATUS.md` concise; target no more than 40 lines.
- In `STATUS.md`, report verification totals and failures, not command output or
  detailed benchmark samples.
- Update a PBI with its final status and material scope changes only. Do not add
  a diary of how it was implemented.
- Add a decision only for a durable product or architectural constraint that
  affects more than the current implementation detail.
- Keep each decision to its decision, reason, and important consequences.
- Do not repeat the same information across status, decisions, architecture,
  and backlog unless each file needs it for its distinct purpose.
- Preserve unrelated user changes and avoid broad documentation rewrites during
  an implementation PBI.

## Completing a PBI

1. Verify its acceptance criteria and relevant tests.
2. Mark only that PBI complete in `BACKLOG.md`.
3. Replace `STATUS.md` with the new current snapshot.
4. Leave `CURRENT_PBI.md` unchanged until the user explicitly selects the next
   PBI.
5. Summarise changed files, verification, and unresolved issues concisely.
