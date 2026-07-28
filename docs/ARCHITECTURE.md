# Architecture

## Architectural direction

Start with a standalone .NET 10 Blazor WebAssembly application. It downloads as
static assets and executes in the browser, making it suitable for Azure Static
Web Apps, GitHub Pages, or any ordinary static host.

Use an Aspire AppHost for local orchestration and the Aspire dashboard. For
standalone Blazor WebAssembly browser telemetry, use the Aspire Blazor Gateway
as a development-time same-origin proxy. Neither the AppHost nor this gateway
is deployed as the application's production runtime.

Do not create a production ASP.NET Core server project initially. External map
tiles, elevation, geocoding, and routing may be accessed through explicit
provider adapters. A small production gateway or self-hosted routing service
can be introduced later without changing the domain model.

## Suggested solution

```text
RouteTrace.slnx
├─ src/
│  ├─ RouteTrace.AppHost/
│  ├─ RouteTrace.Core/
│  │  ├─ Geography/
│  │  ├─ Gpx/
│  │  ├─ Projects/
│  │  └─ Routes/
│  └─ RouteTrace.Web/
│     ├─ Features/
│     │  ├─ Map/
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

Create `AppHost`, `Core`, `Web`, and `Core.Tests` initially. `TestData` is a
fixture directory, not necessarily a project. Add `Web.Tests`,
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

Pin a compatible current Aspire version during PBI-000. The standalone Blazor
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
RouteProject
├─ Name
├─ Source information
├─ RouteDocument
│  ├─ Tracks[]
│  │  └─ Segments[]
│  │     └─ RoutePoint[]
│  ├─ Routes[]
│  └─ Waypoints[]
├─ ImageOverlay?
├─ Editing anchors[]
└─ Provider settings
```

`RoutePoint` starts with latitude, longitude, optional elevation, and optional
time. Additional fields should be introduced only when a PBI consumes them.

Maintain the distinction between:

- A track segment break, where points must not be connected.
- A continuous route line.
- Independent waypoints/POIs.
- Editing anchors used to reproduce a routed path.

Unknown GPX extensions should be preserved as opaque XML where feasible.
Round-trip preservation and semantic understanding are separate concerns.

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

Use IndexedDB for projects and imported images. `localStorage` is appropriate
only for small preferences such as the last selected map style.

Persist a versioned application-owned project document, not raw component
state. Schema migrations can then be applied when reopening an older project.

An exported project bundle may be added later to provide backup and transfer
without accounts.

## Testing

- Unit-test GPX parsing/writing and geometry in `Core.Tests`.
- Use fixed GPX fixtures representing tracks, multiple segments, routes,
  waypoints, elevation, timestamps, and unknown extensions.
- Test provider adapters using recorded JSON fixtures rather than live services.
- Add browser/component tests for user-visible workflows after the map shell is
  stable.
- Maintain a small device-compatibility fixture set for Wahoo and Garmin.

## Deployment

The Web project publishes static assets. AppHost is used by development and
tests but is not included in the static deployment. The initial CI pipeline
should:

1. Restore tools and packages.
2. Build TypeScript assets if present.
3. Build and test .NET.
4. Publish the standalone WebAssembly application.
5. Deploy the publish output.

Azure Static Web Apps is a suitable initial host, but the application should
not depend on Azure-specific runtime features.

## Deferred decisions

- Product and solution name.
- OpenLayers versus MapLibre after a focused spike.
- Tile/style provider and cycling-map presentation.
- Routing and map-matching provider.
- Whether a hosted gateway becomes necessary.
- Computer-vision library.
- FIT SDK integration and licensing/distribution implications.
