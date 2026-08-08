# Architecture

## Architectural direction

Start with a standalone .NET 10 Blazor WebAssembly application. It downloads as
static assets and executes in the browser, making it suitable for Azure Static
Web Apps, GitHub Pages, or any ordinary static host.

Aspire orchestration and browser telemetry are deferred to PBI-210. If added,
they remain optional development infrastructure and are not deployed as the
application's production runtime.

Do not create a production ASP.NET Core server project initially. External map
tiles, elevation, geocoding, and routing may be accessed through explicit
provider adapters. A small production gateway or self-hosted routing service
can be introduced later without changing the domain model.

## Suggested solution

```text
RouteTrace.slnx
├─ src/
│  ├─ RouteTrace.Core/
│  │  ├─ Geography/
│  │  ├─ Gpx/
│  │  ├─ Projects/
│  │  └─ Routes/
│  └─ RouteTrace.Web/
│     ├─ Features/
│     │  ├─ Map/
│     │  ├─ Workspace/
│     │  ├─ DocumentExplorer/
│     │  ├─ GpxImport/
│     │  ├─ GpxExport/
│     │  ├─ ProjectStorage/
│     │  ├─ ImageOverlay/
│     │  ├─ TraceExtraction/
│     │  └─ RouteMatching/
│     ├─ Components/
│     ├─ Infrastructure/
│     ├─ Scripts/
│     ├─ Styles/
│     └─ wwwroot/
├─ tests/
│  ├─ RouteTrace.Core.Tests/
│  ├─ RouteTrace.Web.Tests/
│  └─ RouteTrace.TestData/
│     ├─ gpx/
│     ├─ images/
│     ├─ expected/
│     └─ README.md
├─ docs/
├─ AGENTS.md
├─ Directory.Build.props
├─ Directory.Packages.props
├─ global.json
└─ README.md
```

Create `Core`, `Web`, and `Core.Tests` initially. `TestData` is a fixture
directory, not necessarily a project. Add `AppHost` only in PBI-210. Add `Web.Tests`,
browser/component, or end-to-end test projects only when behaviour justifies
them. Add a shared ServiceDefaults project only when a non-browser .NET service
exists and can consume it.

## Aspire and telemetry

Aspire provides the local development control plane:

- The AppHost starts the standalone WebAssembly app through the Blazor Gateway.
- The gateway forwards browser OpenTelemetry over a same-origin endpoint to the
  local Aspire dashboard without exposing dashboard credentials to the browser.
- The dashboard is for development diagnostics and is not durable production
  telemetry storage.
- A normal `dotnet publish` of `RouteTrace.Web` must still produce static assets
  that run without AppHost or gateway.

Pin a compatible current Aspire version during PBI-210. The standalone Blazor
hosting integration is currently preview, so verify its .NET 10 compatibility
and record the selected version and any fallback before expanding its use.

If production telemetry is wanted later, select an external OpenTelemetry
backend and a browser-safe ingestion design in a separate PBI. Do not expose a
secret ingestion key in the static client.

## Responsibilities

### RouteTrace.Core

Pure domain and file-format logic:

- Canonical route/project model.
- WGS 84 coordinates and bounds.
- Tracks, segments, route points, and waypoints.
- GPX parsing, validation, and writing.
- Distance, simplification, and geometry algorithms.
- Provider-neutral routing and map-matching request/result types.

It must not reference Blazor, JavaScript interop, browser APIs, map-rendering
libraries, or concrete providers.

### RouteTrace.Web

Browser application and adapters:

- Pages and feature components.
- Map rendering and editing.
- File picker, drag-and-drop, and downloads.
- IndexedDB persistence.
- Image loading and transformation.
- TypeScript/JavaScript interop.
- Concrete routing/elevation adapters.

Organise UI code by feature rather than accumulating all components in generic
folders.

## Canonical model

Do not use generated GPX schema classes as the model edited by the UI. Parse
GPX into a smaller canonical model and serialise from that model.

Suggested concepts:

```text
RouteWorkspace
├─ Name
├─ Documents[]
│  ├─ Id
│  ├─ Source information
│  ├─ RouteDocument
│  │  ├─ Tracks[]
│  │  │  └─ Segments[]
│  │  │     └─ RoutePoint[]
│  │  ├─ Routes[]
│  │  └─ Waypoints[]
│  ├─ Presentation settings
│  ├─ ImageOverlay?
│  └─ Editing anchors[]
├─ ActiveDocumentId?
└─ Provider settings
```

`RouteDocument` and its geographic primitives belong in `RouteTrace.Core`.
`RouteWorkspace` coordinates documents and browser-facing project state without
moving presentation settings into the GPX domain model.

`RoutePoint` starts with latitude, longitude, optional elevation, and optional
time. Additional fields should be introduced only when a PBI consumes them.

Maintain the distinction between:

- A track segment break, where points must not be connected.
- A continuous route line.
- Independent waypoints/POIs.
- Editing anchors used to reproduce a routed path.

Unknown GPX extensions should be preserved as opaque XML where feasible.
Round-trip preservation and semantic understanding are separate concerns.

## Workspace interaction

The application workspace can contain several route documents. Keep these
states distinct:

- **Active:** receives file-level and editing commands.
- **Selected:** highlighted in the explorer or map.
- **Visible:** contributes geometry or markers to the map.

Use stable application-owned document IDs for workspace state; do not derive
identity from filenames or mutable GPX names. GPX content remains canonical
domain data. Colour, visibility, explorer expansion, and derived start/finish
markers are presentation state and are not written to vendor extensions.

Application-menu, keyboard, and context-menu actions should share command
availability and execution rather than duplicate behaviour in components.
Introduce one reusable undo/redo history when geometry editing begins so later
matching and correction workflows do not create incompatible histories.

The document explorer shows semantic structure down to track segments and
waypoints. It should not eagerly create UI nodes for every track point.

## Geographic coordinates

- Domain coordinates: WGS 84 latitude/longitude (`EPSG:4326`).
- Map display is commonly Web Mercator (`EPSG:3857`).
- Convert projections only at map/image adapter boundaries.
- Use `double` for coordinates and geometry calculations.
- Distance and elevation values are expressed internally in SI units.

## Map integration

Use a TypeScript map adapter rather than spreading JavaScript interop calls
through Razor components. The adapter should initially expose operations such
as:

- Initialise and dispose a map.
- Set or fit bounds.
- Set displayed route geometry.
- Set waypoints and editing anchors.
- Add/update/remove an image overlay.
- Report map clicks and geometry edits.

OpenLayers is the leading initial candidate because raster layers, projections,
and vector editing are central to the product. Confirm the choice with a small
spike before committing broadly.

The mapping library is not the map data provider. Tile/style selection,
licensing, attribution, quotas, and cycling layers remain separate decisions.

## TypeScript integration

Keep the npm surface minimal:

- TypeScript source belongs in `RouteTrace.Web/Scripts`.
- Produce browser modules consumed through Blazor JS module interop.
- Do not create a separate SPA framework.
- Add a bundler only when needed by the selected mapping or computer-vision
  dependency.
- Keep TypeScript interfaces at the boundary small and serialisable.

## Provider boundaries

Define provider-neutral interfaces only when a feature first requires them:

- `IRoutePlanner`: route between ordered anchors using a cycling profile.
- `IMapMatcher`: match an approximate directed line to a routable network.
- `IElevationProvider`: add or replace point elevation.

The initial static application may call a remote provider directly. This still
requires careful handling of CORS, quotas, API keys, and key exposure. A
provider requiring a secret cannot be called securely from a static client.

If that becomes limiting, add an optional project:

```text
src/RouteTrace.Gateway/
```

Its scope should be proxying or hosting provider functionality, not absorbing
the browser application or domain logic.

## Local persistence

Use IndexedDB for workspaces and imported images. `localStorage` is appropriate
only for small preferences such as the last selected map style.

Persist a versioned application-owned workspace containing its documents,
stable IDs, active document, and durable presentation settings. Do not persist
raw component state or derived map features. Schema migrations can then be
applied when reopening an older workspace.

An exported project bundle may be added later to provide backup and transfer
without accounts.

## Testing

- Unit-test GPX parsing/writing and geometry in `Core.Tests`.
- Use fixed GPX fixtures representing tracks, multiple segments, routes,
  waypoints, elevation, timestamps, and unknown extensions.
- Test provider adapters using recorded JSON fixtures rather than live services.
- Add browser/component tests for user-visible workflows after the map shell is
  stable.
- Exercise multi-document display and explorer behaviour with existing dense
  and structurally complete GPX fixtures.
- Maintain a small device-compatibility fixture set for Wahoo and Garmin.

## Deployment

The Web project publishes static assets. If AppHost is added in PBI-210, it is
used only by development and tests and is not included in the static
deployment. Until the deployment phase, CI should:

1. Restore tools and packages.
2. Build TypeScript assets if present.
3. Build and test .NET.
4. Publish the standalone WebAssembly application.
5. Retain or smoke-test the publish output without deploying it.

PBI-220 defines Azure Static Web Apps through Bicep. PBI-230 adds deployment
after all required checks pass. The application must remain an ordinary static
site and must not depend on Azure-specific runtime features.

## Deferred decisions

- Product and solution name.
- Tile/style provider and cycling-map presentation.
- Routing and map-matching provider.
- Whether a hosted gateway becomes necessary.
- Computer-vision library.
- FIT SDK integration and licensing/distribution implications.
