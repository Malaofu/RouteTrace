# Decisions

Record decisions that should survive beyond the current PBI. Keep implementation
notes and temporary findings in pull requests or task notes instead.

## D-001 — Static-first standalone web application

**Status:** Accepted
**Date:** 2026-07-26

The initial product will be a standalone .NET 10 Blazor WebAssembly
application. It must publish as static assets and must not require an
application server.

External routing, map tiles, and elevation may be consumed through provider
adapters. A gateway can be added later if provider credentials, CORS, quotas,
or self-hosting require it.

## D-002 — Local-first project and image handling

**Status:** Accepted  
**Date:** 2026-07-26

Imported images, GPX parsing, project state, and image processing remain in the
browser by default. Projects will use IndexedDB and require no account.

Only coordinate data required by an explicitly selected external routing or
elevation provider may leave the browser.

## D-003 — Progressive automation

**Status:** Accepted  
**Date:** 2026-07-26

The product will first support manual routed tracing over an aligned image.
Automatic colour extraction, vectorisation, and map matching will be added
after the manual workflow is useful.

Human review is part of the intended solution. The product will expose
ambiguity rather than claiming perfect automatic recognition.

## D-004 — Canonical internal route model

**Status:** Accepted  
**Date:** 2026-07-26

The editable application model is independent of GPX schema classes, Blazor,
the map renderer, and route providers.

GPX, TCX, FIT, map-library objects, and provider responses are boundary
formats translated to or from the canonical model.

## D-005 — Standard GPX as baseline output

**Status:** Accepted  
**Date:** 2026-07-26

The first export format is GPX 1.1 containing portable route geometry and
waypoints. A valid GPX route does not depend on embedded turn cues.

TCX and FIT Course output will be evaluated later for richer navigation
metadata and device interoperability.

## D-006 — Aspire is development infrastructure

**Status:** Superseded by D-008
**Date:** 2026-07-28

This decision originally assigned an Aspire AppHost and the Aspire Blazor
Gateway to the repository baseline. D-008 defers that work to PBI-210.

AppHost and the development gateway are not a production application server.
The Web project must continue to publish and run as static assets without them.
Production telemetry, if later required, needs a separate browser-safe ingestion
and retention decision.

## D-007 — Current PBI pointer and fixture gate

**Status:** Accepted
**Date:** 2026-07-28

`docs/CURRENT_PBI.md` contains only one `PBI-NNN` identifier. Codex resolves the
complete scope and acceptance criteria from the corresponding section in
`docs/BACKLOG.md`.

Before implementation, Codex checks `docs/FIXTURES.md` and reports any files or
real-device evidence the user must provide. Fixture-dependent work waits for
those inputs or explicit approval to use a synthetic substitute.

## D-008 — Aspire telemetry is deferred

**Status:** Accepted
**Date:** 2026-07-28

Aspire AppHost, the Blazor Gateway, and browser telemetry are not part of the
repository baseline in PBI-000. They are deferred to PBI-210 and may be
scheduled independently of product delivery.

If implemented, they remain development-only infrastructure. The Web project
must continue to publish and run as static assets without AppHost, a gateway,
or a telemetry backend.

## Decision template

```markdown
## D-NNN — Short title

**Status:** Proposed | Accepted | Superseded
**Date:** YYYY-MM-DD

Context and decision.

Consequences and constraints.
```
