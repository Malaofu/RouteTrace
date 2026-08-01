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

## D-009 — OpenLayers with OpenStreetMap development tiles

**Status:** Accepted
**Date:** 2026-07-29

Use OpenLayers 10.10.0 behind a feature-scoped TypeScript adapter. OpenLayers
was selected over MapLibre GL JS because Route Trace prioritises raster/image
layers, explicit projections, and editable vector geometry; OpenLayers exposes
these directly, while MapLibre is primarily organised around GPU-rendered
vector styles. Razor components may call the adapter but must not call
OpenLayers APIs directly.

Use the standard OpenStreetMap raster endpoint for light development and early
manual testing only. Keep its URL inside the adapter so it can be replaced
without changing Razor components. The map must show OpenStreetMap attribution,
honour normal browser caching and referrer behaviour, and must not add tile
prefetch, bulk download, or offline-map features. The service is best-effort
and has no SLA, so select a suitable hosted or self-hosted provider before
expecting material public traffic.

Bundle the adapter and OpenLayers CSS with esbuild. TypeScript remains strict,
and the npm surface is limited to OpenLayers, TypeScript, and esbuild.

## D-010 — App-owned theme and styling foundation

**Status:** Accepted
**Date:** 2026-07-29

Use a small app-owned design system based on semantic CSS custom properties
instead of Bootstrap or another component framework. Light and dark palettes
are first-class, and the user can select Light, Dark, or Auto. Auto follows the
browser's operating-system preference. Store only the theme preference in
browser local storage and apply it before Blazor starts to avoid a flash of the
wrong theme.

Author global styles in structured SCSS modules and compile them to expanded,
readable static CSS in the existing npm build. Author component styles as
matching `.razor.scss` files. The build discovers these files automatically and
generates `.razor.css` intermediates under `.generated/scopedcss-input`,
preserving project-relative paths. One wildcard `ScopedCssInput` maps the
generated files back to their components before Blazor performs CSS isolation.
The ignored staging tree sits outside `wwwroot`, so the raw inputs are not
published. Add reusable controls only as product PBIs need them; the shared
tokens for colour, spacing, radii, focus states, and elevation are the stable
foundation.

Keep executable scripts and SVG artwork in dedicated resource files. HTML and
Razor files reference those resources rather than embedding them inline.

## D-011 — Minimal immutable route primitives

**Status:** Accepted
**Date:** 2026-07-30

The canonical route model uses small, read-only WGS 84 domain primitives.
Tracks contain ordered segments, and each segment is continuous; a segment
boundary is the explicit representation of a discontinuity. Routes and
waypoints share the same point representation, whose elevation and timestamp
are optional.

Coordinates reject non-finite and out-of-range values at construction.
Collections are defensively copied, and document bounds are derived across
track points, route points, and waypoints. GPX metadata and extension fields
remain boundary concerns until a product PBI demonstrates that they belong in
the editable model.

## D-012 — Consistent .NET test stack

**Status:** Accepted
**Date:** 2026-07-30

.NET test projects use xUnit.net v3 as the test framework, Shouldly for
assertions, FakeItEasy for test doubles, and Coverlet's collector for code
coverage. Package versions are centrally managed, and every test project
references the same stack.

Test-runner and coverage packages remain private assets. Production projects
must not reference test packages, and tests should not introduce a fake when a
small real value or implementation communicates the behaviour more clearly.

## D-013 — GitHub-native test and coverage reporting

**Status:** Accepted
**Date:** 2026-07-30

CI emits TRX and Cobertura files from the existing .NET test run. A test
reporter presents TRX results as a GitHub check and job summary, while a pinned
repository-local ReportGenerator tool creates a combined Markdown coverage
summary and detailed HTML report. Raw and rendered reports are retained as a
short-lived workflow artifact.

This provides useful reporting without an external coverage service, account,
or token. Check creation is skipped for fork pull requests because their
workflow token is read-only; their test output, coverage summary, and artifacts
remain available in the workflow run.

## D-014 — Browser-independent GPX import boundary

**Status:** Accepted
**Date:** 2026-08-01

Parse GPX 1.1 in `RouteTrace.Core` from a stream into the canonical route
model. The parser has no browser or UI dependencies, prohibits DTD processing,
validates numeric coordinates and optional values, and returns user-readable
failures instead of exposing XML exceptions.

The Web project supplies only the browser-local file stream and displays the
result. Unsupported namespaced elements inside GPX extension containers are
retained as opaque XML so vendor data is not silently reinterpreted or lost.

## D-015 — Map geometry projection and feature identity

**Status:** Accepted
**Date:** 2026-08-01

The Web feature maps the canonical document to a serialization-only geometry
payload while retaining track and segment boundaries. The OpenLayers adapter
performs the WGS 84-to-Web Mercator projection and creates one feature per
segment, preventing discontinuities from being joined visually.

Track and segment indices provide local feature identity for highlighting.
This avoids introducing editable entity identifiers before an editing PBI
requires them, while keeping OpenLayers types out of the domain model.

## D-016 — Conservative route statistics

**Status:** Accepted
**Date:** 2026-08-01

Route statistics are calculated in `RouteTrace.Core` from canonical WGS 84
geometry. Distances use a spherical haversine calculation and are accumulated
within individual track segments only; segment boundaries never create an
implied connecting distance or elevation change.

Elevation range may use the available samples, but ascent and descent are
reported only when every track point has elevation. Missing elevation and time
remain absent rather than becoming zero. Extension elements are reported by
their distinct namespace URI without interpreting vendor-specific content.

## D-017 — Extract extension namespaces during GPX import

**Status:** Accepted
**Date:** 2026-08-01

The GPX importer copies the browser file asynchronously into bounded in-memory
storage, then reads that buffer forward-only. Track, route, and waypoint data
is parsed directly from the reader; only the small metadata subtree is
materialised. This keeps browser I/O async-only without paying for an async
continuation at every XML node. It records distinct unsupported-extension
namespace names while each small subtree is available. The canonical document
retains both the opaque extension XML and this small derived index, so statistics and UI
consumers do not reparse every extension fragment. Documents constructed by
other callers derive the same index from their opaque XML for compatibility.

GPX loading therefore retains one byte buffer but does not retain insignificant
whitespace, construct thousands of point XML trees, or duplicate the file as a
large XML DOM. Unsupported extension fragments remain opaque and complete.
This reduces browser CPU cost without interpreting or dropping vendor data.

## D-018 — Browser-measured loading performance

**Status:** Accepted
**Date:** 2026-08-01

Large-file performance is verified end to end with Playwright against the
actual Blazor WebAssembly application and full-density fixture. Browser
performance marks separate parsing, component propagation, inspector rendering,
interop, and OpenLayers feature construction. CI installs Chromium and enforces
the complete import-to-render budget; local Windows runs use the installed Edge.
The import panel commits an accessible busy state before CPU-heavy parsing and
uses a compositor-friendly activity indicator. Browser tests separately enforce
that feedback appears within 100 ms, so perceived responsiveness does not rely
only on the eventual completion budget.

Opaque extension XML is retained lazily from the browser-local source buffer.
Import records extension namespaces eagerly for the inspector, while the 6,987
individual XML fragments in the performance fixture are materialised only when
a consumer such as export requests them.

## D-019 — AOT-compile published WebAssembly

**Status:** Accepted
**Date:** 2026-08-01

Release publishing uses WebAssembly ahead-of-time compilation; Debug builds
remain interpreted for fast iteration. Browser tests run against the published
AOT artifact and enforce a 500 ms full-density import-to-render budget.

Local Edge measurements improved from roughly 1.3–1.4 seconds interpreted to
182 ms AOT, with parsing falling from about 1,050 ms to 102 ms. The tradeoff is
a clean publish time of roughly 102 seconds and an increase in Brotli-compressed
published assets from approximately 3.01 MiB to 5.61 MiB. The application is
installable and service-worker cached, so repeat visits amortise the larger
initial download.

## D-020 — Export opaque extensions at document scope

**Status:** Accepted
**Date:** 2026-08-01

GPX export is implemented in `RouteTrace.Core` as an ordered, forward-only GPX
1.1 writer. It preserves the canonical tracks, segment boundaries, routes,
waypoints, elevation, time, and metadata without depending on browser APIs.
The Web application supplies the required creator and adapts the resulting
stream to a local browser download.

Opaque extension XML retains its GPX owner path in a lazy index, allowing
export to restore metadata, route, track, segment, and point extensions to
their schema-valid containers. Building that index and materialising the XML
is deferred until export requests it, preserving large-file import performance.
Prefixed namespace declarations from the imported GPX root are restored on the
exported root so vendor prefixes such as `gpxtpx` remain available to the
extension elements and compatibility-oriented consumers. Redundant declarations
introduced while extracting opaque fragments are removed during export, so the
root declaration remains the single declaration when it is already in scope.
The export result explicitly reports retained and omitted extensions; the
current implementation omits none.

Coordinates remain numeric WGS 84 values in the canonical model. GPX export
always formats latitude and longitude with exactly seven fractional digits,
independent of the lexical formatting used by the imported file.
Elevation is normalised to include at least one fractional digit for consistent
device-friendly output, so an integral value is written as `47.0` rather than
`47`.

Standard GPX fields that Route Trace does not yet interpret are retained in a
shared lazy owner-path snapshot and written back at their original schema scope
and order. Metadata, routes, the document, and the exporter expose views over
that same snapshot rather than copying extension XML into separate stores. The
snapshot parses the source once and retains XML elements directly, avoiding a
serialize/parse cycle for every preserved field and extension. It is built only
when preserved content is inspected or exported, so ordinary import remains
streaming and does not materialise every point subtree.

Browser performance marks measure serialization separately from download
interop. On the 6,987-point fixture, consolidation reduced interpreted export
from about 5.70 seconds to roughly 2.1–3.4 seconds locally, depending on browser
and test-suite state. The AOT published application exports in about
170 ms, including roughly 23 ms of browser download handoff. Tests enforce a
500 ms AOT budget and a wider 5-second interpreted development budget.

Browser downloads use the metadata name when it is present and fall back to
the imported filename, then to `route-trace.gpx`. Names are stripped of
cross-platform filename metacharacters and receive a `.gpx` extension when
needed. This keeps naming a UI adapter concern rather than adding source-file
identity to the canonical route document.

## Decision template

```markdown
## D-NNN — Short title

**Status:** Proposed | Accepted | Superseded
**Date:** YYYY-MM-DD

Context and decision.

Consequences and constraints.
```
