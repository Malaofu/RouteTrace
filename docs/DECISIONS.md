# Decisions

This file contains current, durable product and architecture decisions. Git
history preserves superseded text; implementation details belong in code,
tests, commits, or pull requests.

## D-001 — Static-first standalone web application

**Status:** Accepted  
**Date:** 2026-07-26

Route Trace is a .NET 10 Blazor WebAssembly application that publishes as static
assets. A gateway may be added for provider constraints, but the browser app and
domain model must not depend on a production application server.

## D-002 — Local-first data handling

**Status:** Accepted  
**Date:** 2026-07-26

GPX processing, images, and workspace data remain in the browser by default and
require no account. Only data required by a user-selected external provider may
leave the browser.

## D-003 — Progressive automation

**Status:** Accepted  
**Date:** 2026-07-26

Manual routed tracing must become useful before automatic image extraction and
map matching. Uncertain automatic results require explicit user review.

## D-004 — Canonical internal route model

**Status:** Accepted  
**Date:** 2026-07-26

Editable route data is independent of GPX schema classes, Blazor, OpenLayers,
and provider responses. External file formats and provider models are adapters
around the canonical model.

## D-005 — GPX 1.1 is the baseline output

**Status:** Accepted  
**Date:** 2026-07-26

Standard GPX 1.1 is always available for portable geometry and waypoints. TCX
and FIT Course may later add richer navigation semantics without replacing GPX.

## D-007 — Current PBI pointer and fixture gate

**Status:** Accepted  
**Date:** 2026-07-28

`CURRENT_PBI.md` contains exactly one `PBI-NNN` identifier. Codex implements
only its matching backlog section and reports required fixtures before
fixture-dependent work begins.

## D-008 — Aspire is optional development infrastructure

**Status:** Accepted  
**Date:** 2026-07-28

Aspire AppHost, browser telemetry, and any development gateway are deferred to
PBI-210. They must not become production runtime dependencies or expose
telemetry credentials to the browser.

## D-009 — OpenLayers behind a TypeScript adapter

**Status:** Accepted  
**Date:** 2026-07-29

Use OpenLayers 10.10.0 behind a feature-scoped TypeScript adapter because the
product depends on raster overlays, projections, and editable vector geometry.
OpenStreetMap raster tiles are for light development use with attribution, not
bulk access or a production SLA.

## D-010 — App-owned styling foundation

**Status:** Accepted  
**Date:** 2026-07-29

Use semantic CSS custom properties and SCSS rather than a component framework.
Light, dark, and automatic themes are first-class; component SCSS is compiled
into Blazor CSS isolation through the existing npm build.

## D-011 — Minimal immutable route primitives

**Status:** Accepted  
**Date:** 2026-07-30

Core route primitives are immutable WGS 84 values with validated coordinates
and defensively copied collections. Segment boundaries explicitly represent
discontinuities; elevation and time remain optional.

## D-012 — Consistent .NET test stack

**Status:** Accepted  
**Date:** 2026-07-30

.NET tests use xUnit.net v3, Shouldly, FakeItEasy where a test double is useful,
and Coverlet for coverage. Versions are centrally managed and test packages do
not flow into production projects.

## D-013 — GitHub-native test reporting

**Status:** Accepted  
**Date:** 2026-07-30

CI publishes TRX checks plus Cobertura summaries and short-lived detailed
artifacts. This avoids an external reporting account or token.

## D-014 — Browser-independent GPX import

**Status:** Accepted  
**Date:** 2026-08-01

`RouteTrace.Core` parses GPX 1.1 streams without browser dependencies, prohibits
DTD processing, validates values, and returns user-readable failures. Unknown
extension content is preserved rather than silently interpreted or discarded.

## D-015 — Projection and map feature identity stay at the boundary

**Status:** Accepted  
**Date:** 2026-08-01

Canonical coordinates remain WGS 84. The map adapter projects to Web Mercator
and renders segments separately so discontinuities are not connected. Stable
workspace IDs may supplement local indices when editing requires identity.

## D-016 — Conservative route statistics

**Status:** Accepted  
**Date:** 2026-08-01

Distance is accumulated within segments using a spherical haversine
calculation. Missing elevation or time remains absent, and ascent/descent is not
reported from incomplete elevation data.

## D-017 — Streaming import with lazy preserved content

**Status:** Accepted  
**Date:** 2026-08-01

GPX import uses bounded browser-local buffering and forward-only parsing rather
than a full document DOM. Unsupported standard fields and extensions retain
owner information and are materialised only when inspected or exported.

## D-018 — Browser-measured performance

**Status:** Accepted  
**Date:** 2026-08-01

Large-file budgets are enforced end to end in the real WebAssembly application
with Playwright. Accessible loading feedback has a separate responsiveness
budget from eventual completion.

## D-019 — AOT-compiled Release WebAssembly

**Status:** Accepted  
**Date:** 2026-08-01

Release publishing uses WebAssembly AOT; Debug remains interpreted for fast
iteration. CI must install `wasm-tools` and run performance checks against the
published AOT artifact rather than disabling AOT to simplify the pipeline.

## D-020 — Owner-scoped GPX round-trip export

**Status:** Accepted  
**Date:** 2026-08-01

`RouteTrace.Core` writes ordered GPX 1.1 and restores preserved standard fields,
namespace declarations, and opaque extensions to their schema-valid owners.
Export reports omissions explicitly and uses deterministic coordinate and
elevation formatting.

## D-021 — Multi-document workspace and separate presentation state

**Status:** Accepted  
**Date:** 2026-08-08

A workspace contains stable document IDs and multiple canonical route
documents. Active, selected, and visible are independent states. Colour,
visibility, explorer state, and derived endpoint markers are project-local
presentation data and are not written into GPX vendor extensions.

## Decision template

```markdown
## D-NNN — Short title

**Status:** Proposed | Accepted
**Date:** YYYY-MM-DD

Decision, reason, and important consequences in one short paragraph.
```
