# Product backlog

This is a planning document. Implementation agents should use the identifier in
`CURRENT_PBI.md` to read exactly one matching section, not this entire file.

Status values: `Not started`, `In progress`, `Blocked`, `Done`.

## PBI-000 — Repository and static application baseline

**Status:** Done

**Goal:** Establish a minimal .NET 10 solution that builds, tests, and publishes
the Web project as static assets.

**Tasks:**

- Create the solution and `Core`, `Web`, and `Core.Tests` projects.
- Enable nullable references, warnings, central package management, formatting,
  and deterministic builds.
- Add a minimal application shell and error boundary.
- Add build/test/publish commands to the README.
- Add CI that builds and tests; deployment can remain manual.
- Verify the published WebAssembly output can be served by a plain static file
  server.

**Acceptance criteria:**

- A clean clone can build and test with documented commands.
- Published output works from a static server.
- No production server/API, map package, authentication, or database has been
  added.

## PBI-010 — Interactive map shell

**Status:** Done

**Goal:** Display a world map suitable for later route and image overlays.

**Tasks:**

- Spike OpenLayers and MapLibre only as far as needed to choose one.
- Add a narrow TypeScript map adapter.
- Support pan, zoom, resize, and fit-to-bounds.
- Show required data-provider attribution.
- Select an initial development tile/style source.

**Acceptance criteria:**

- The map fills its intended application area and responds correctly to resize.
- Pan and zoom work on desktop.
- Attribution is visible.
- Razor components do not make low-level map-library calls.
- The selection and provider constraints are recorded in `DECISIONS.md`.

## PBI-020 — Canonical route model

**Status:** Done

**Goal:** Define the provider- and UI-independent representation used by later
GPX and editing features.

**Tasks:**

- Add coordinates, bounds, route documents, tracks, segments, points, routes,
  and waypoints.
- Represent optional elevation and time.
- Represent segment discontinuities explicitly.
- Add basic model validation and bounds calculation.

**Acceptance criteria:**

- `Core` contains no browser or map-library dependencies.
- Tests cover empty documents, multiple tracks, multiple segments, bounds, and
  invalid coordinates.
- The model does not prematurely include every GPX schema field.

## PBI-030 — GPX import and validation

**Status:** Done

**Goal:** Import GPX 1.1 into the canonical model and report useful errors.

**Tasks:**

- Add browser file selection and drag-and-drop.
- Parse metadata, tracks, segments, route points, waypoints, elevation, and
  timestamps.
- Preserve unsupported extension XML where practical.
- Reject malformed XML and invalid coordinates with user-readable errors.
- Add representative fixtures.

**Acceptance criteria:**

- Valid GPX files create the expected canonical document.
- Multiple tracks and segments remain distinct.
- Invalid input does not crash the application.
- Imported files are processed locally.
- Parser tests do not require a browser.

## PBI-040 — GPX map visualisation

**Status:** Done

**Goal:** Display imported tracks, routes, segment breaks, and waypoints.

**Tasks:**

- Convert WGS 84 domain geometry at the map-adapter boundary.
- Render all supported geometry types distinctly.
- Fit the map to imported content.
- Provide selection/highlighting for a track or segment.

**Acceptance criteria:**

- A multi-track fixture renders correctly.
- Disconnected track segments are not joined visually.
- Waypoints appear at the correct locations.
- Importing an empty document produces an intentional empty state.

## PBI-050 — GPX inspector and statistics

**Status:** Done

**Goal:** Explain what is inside an imported GPX file.

**Tasks:**

- Display metadata and counts for tracks, routes, segments, points, and
  waypoints.
- Calculate distance per segment and total distance.
- Display elevation range and ascent/descent only when meaningful.
- Display time range/duration when timestamps exist.
- Identify extension namespaces without attempting to understand all of them.

**Acceptance criteria:**

- Calculations are covered by fixed tests.
- Missing elevation/time is represented as missing, not zero.
- Segment gaps do not contribute a straight-line distance.
- The user can correlate an inspector item with map highlighting.

## PBI-051 — Loading performance

**Status:** Done

**Goal:** Load large GPX files in a reasonable time

**Tasks:**

- Figure out why files with many points loads very slowly
- Improve the performance of loading large gpx files

**Acceptance criteria:**

- Import of large gpx files run in reasonable time

## PBI-060 — GPX export and round trip

**Status:** Done

**Goal:** Export the canonical document as valid GPX 1.1.

**Tasks:**

- Write ordered GPX 1.1 XML with a required creator.
- Export through a browser download.
- Preserve tracks, segments, routes, waypoints, elevation, and time.
- Preserve supported opaque extensions.
- Validate output against the GPX schema in tests.

**Acceptance criteria:**

- Import → export → import retains the supported semantic model.
- Separate segments remain separate.
- Output opens in at least one independent GPX viewer.
- Unknown extensions are either retained or explicitly reported as omitted.

## PBI-061 — Restore Release AOT CI

**Status:** Complete

**Goal:** Make the clean CI runner build and publish the existing AOT-compiled
Blazor WebAssembly application.

**Tasks:**

- Install the .NET `wasm-tools` workload after selecting the SDK and before the
  Release build.
- Preserve the repository's existing action-version pinning convention.
- Run the existing format, build, test, publish, and browser-performance checks.
- Document the corresponding local prerequisite without weakening AOT.

**Acceptance criteria:**

- A clean CI run completes the Release build and publish.
- The published application remains WebAssembly AOT compiled.
- Existing .NET and Playwright checks still pass.
- No fixture is required and no product behaviour changes.

## PBI-070 — Browser-local workspace persistence

**Status:** Complete

**Goal:** Save and reopen a multi-document workspace without login or cloud
storage.

**Tasks:**

- Define a workspace containing stable document IDs, multiple canonical route
  documents, and an active-document ID.
- Define a versioned workspace-storage DTO and store it in IndexedDB.
- Persist workspace updates automatically and restore the most recently active
  workspace on startup.
- List, name, reopen, and delete saved workspaces.
- Handle incompatible/corrupt saved data without breaking the application.
- Keep derived map features and component state out of the persisted format.

**Acceptance criteria:**

- A workspace containing more than one route document survives browser refresh
  and application restart without an explicit save or reopen action.
- Workspace deletion requires deliberate user action.
- Storage schema version is explicit.
- Images are not yet persisted unless required by this PBI's implementation.

## PBI-071 — Application menu and command surface

**Status:** Complete

**Goal:** Give file, edit, and view actions a central, extensible home without
covering the map with permanent controls.

**Tasks:**

- Add a compact application menu suitable for desktop and narrow layouts.
- Move existing import and export actions into the File menu.
- Share command availability between menus, keyboard shortcuts, and any
  contextual buttons.
- Implement keyboard navigation, focus management, and dismissal behaviour.

**Acceptance criteria:**

- Existing GPX import and export remain available from one predictable place.
- Disabled or unavailable commands cannot execute.
- The menu is usable by keyboard and does not interfere with map interaction.
- New commands can be added without adding permanent map controls.

## PBI-072 — Multi-document workspace

**Status:** Complete

**Goal:** Open and compare several GPX documents on the same map.

**Tasks:**

- Import one or more GPX files into the current workspace without replacing
  existing documents.
- Track active, selected, and visible state independently.
- Render every visible document with a distinguishable default style.
- Activate, close, and export individual documents.
- Preserve the last valid workspace when one import fails.

**Acceptance criteria:**

- At least three imported GPX documents can be displayed simultaneously.
- Changing the active document does not hide the other visible documents.
- Closing one document does not alter another document's canonical data.
- Export targets the intended document.

## PBI-073 — GPX document explorer

**Status:** Complete

**Goal:** Show the structure of every open GPX document in a coherent workspace
panel.

**Tasks:**

- Add a collapsible right-side explorer, using a drawer on narrow layouts.
- Keep the desktop explorer beside the map so collapsing it returns its width
  to the map; preserve expansion independently from active selection.
- Show document, waypoint, route, track, and segment hierarchy with useful
  names and counts.
- Synchronise explorer selection with map highlighting and focus.
- Keep individual track points out of the normal tree and avoid eager creation
  of thousands of UI nodes.

**Acceptance criteria:**

- The complete GPX fixture can be understood from the explorer hierarchy.
- Selecting a route, track, segment, or waypoint identifies it on the map.
- The full-density fixture does not create a tree item per track point.
- The explorer can be hidden without losing workspace state.
- Clicking a semantic row makes it active without changing expanded nodes;
  visibility is represented consistently at each semantic level.
- Selecting a document, track, or waypoint group represents its complete
  semantic subtree for highlighting and later context actions.

## PBI-074 — Context actions and presentation settings

**Status:** Done

**Goal:** Provide node-specific actions and project-local display settings from
the document explorer.

**Tasks:**

- Expose the same actions through a pointer-positioned right-click menu and
  keyboard context-menu invocation.
- Support activate, focus, show/hide, colour, export, and close where applicable.
- Model document and child visibility and colour overrides with clear
  inheritance.
- Show effective inherited colours in the explorer and allow child overrides
  to reset to their parent.
- Edit supported GPX text through Info dialogs and persist edits across reloads.
- Persist presentation settings in the workspace rather than vendor GPX
  extensions.

**Acceptance criteria:**

- Available actions match the selected node type.
- Colour and visibility changes affect display without changing exported GPX.
- Context actions are usable without a mouse.
- Future editing commands can use the same command surface.

## PBI-075 — Route endpoints and POI symbols

**Status:** Not started

**Goal:** Make route direction and imported points of interest immediately
recognisable on the map.

**Tasks:**

- Add derived start and finish markers for displayed tracks and routes.
- Handle overlapping endpoints for loops without obscuring both meanings.
- Map common GPX waypoint `sym` values through an application-owned icon
  catalogue.
- Use a generic fallback for missing or unknown symbols.
- Keep start/finish markers as presentation data and preserve waypoint symbols
  as GPX data.

**Acceptance criteria:**

- Direction endpoints are legible at ordinary map zoom levels.
- Known, unknown, and missing waypoint symbols render safely.
- Multi-segment tracks receive one track start and finish, not markers on every
  segment boundary.
- Export does not add device-specific icon extensions.

## PBI-076 — Existing-elevation profile

**Status:** Not started

**Goal:** Visualise elevation already present in an imported GPX document
without calling an elevation provider.

**Tasks:**

- Plot elevation against cumulative distance for the active selection.
- Represent missing elevation as gaps rather than zero.
- Synchronise chart hover/focus with a map position where practical.
- Make the profile collapsible and responsive.

**Acceptance criteria:**

- Complete, partial, and absent-elevation fixtures are represented honestly.
- Track segment boundaries do not create implied distance or elevation changes.
- The feature performs no network request.
- PBI-076 may be deferred without blocking manual route editing.

## PBI-080 — Manual route editing

**Status:** Not started

**Goal:** Allow a user to create and adjust an unrouted line on the map.

**Tasks:**

- Add, move, insert, and delete ordered editing points.
- Choose or reverse direction and select a starting point for a loop.
- Establish reusable undo/redo history for the editing workflows that follow.
- Clear recent edits without bypassing that history.
- Convert the edited line into canonical track geometry.

**Acceptance criteria:**

- The complete edit workflow works without a routing provider.
- Direction is visible.
- Undo and redo cover the operations introduced by this PBI.
- The result exports as GPX.

## PBI-090 — Bicycle routing between anchors

**Status:** Not started

**Goal:** Replace straight lines between selected anchors with bicycle-routed
geometry.

**Tasks:**

- Define the minimum provider-neutral routing contract.
- Integrate one bicycle-routing provider.
- Retain anchors separately from calculated geometry.
- Display progress, failure, and no-route states.
- Recalculate only affected legs after an anchor edit where practical.

**Acceptance criteria:**

- Ordered anchors produce a continuous bicycle route.
- Moving one anchor updates the result.
- Provider failure does not destroy the last valid route.
- Provider choice and client-side credential implications are documented.

## PBI-100 — Local image import and overlay

**Status:** Not started

**Goal:** Display a user-supplied route image above the map without uploading it.

**Tasks:**

- Accept common browser-supported raster formats.
- Validate file type, image dimensions, and a reasonable size limit.
- Display the image as an overlay with an opacity control.
- Remove and replace the image.

**Acceptance criteria:**

- The imported image remains local.
- The map is usable through or around the overlay.
- Large/invalid files produce useful feedback.
- Removing the image releases browser resources.

## PBI-110 — Image placement and calibration

**Status:** Not started

**Goal:** Align the imported image with the geographic map.

**Tasks:**

- Support translation, scale, rotation, and opacity.
- Add a simple control-point calibration workflow.
- Persist the image transformation as project data.
- Show enough controls to refine alignment without obscuring the map.

**Acceptance criteria:**

- Alignment survives reopening a saved project.
- At least two known points can establish a useful transform.
- Coordinates and projection conversions are isolated from UI components.
- Perspective/warping beyond the selected transform is explicitly deferred.

## PBI-120 — Manual routed tracing over an image

**Status:** Not started

**Goal:** Recreate an image route by placing a relatively small number of
anchors and routing between them.

**Tasks:**

- Combine image overlay, route anchors, and bicycle routing in one workflow.
- Make overlay visibility/opacity quick to toggle.
- Allow forcing additional anchors where the router chooses the wrong road.
- Allow reversing the complete route.

**Acceptance criteria:**

- A representative route image can be recreated and exported.
- The user can correct an incorrect routed leg.
- The workflow requires significantly fewer points than tracing the complete
  geometry manually.
- This PBI does not attempt automatic pixel extraction.

## PBI-130 — Route-colour segmentation

**Status:** Not started

**Goal:** Produce an editable pixel mask for a visually distinct route line.

**Tasks:**

- Evaluate a client-side image-processing implementation.
- Let the user sample the route colour.
- Provide tolerance and cleanup controls.
- Preview the resulting binary mask.
- Keep processing off the UI thread where necessary.

**Acceptance criteria:**

- A defined fixture set produces usable masks.
- Processing occurs locally.
- The user can correct imperfect colour selection.
- This PBI outputs a mask, not geographic route geometry.

## PBI-140 — Centreline and ordered pixel trace

**Status:** Not started

**Goal:** Convert a route mask into a simplified, directed pixel-space line.

**Tasks:**

- Close small gaps and remove small isolated regions.
- Skeletonise the mask.
- Build a graph from the skeleton.
- Let the user choose start and direction.
- Detect branches, crossings, and ambiguous ordering.
- Simplify the selected pixel path.

**Acceptance criteria:**

- Simple open and loop fixtures produce ordered paths.
- Crossings/branches are reported rather than silently resolved.
- Start and direction can be corrected.
- The result remains in image-pixel coordinates.

## PBI-150 — Geographic extracted trace

**Status:** Not started

**Goal:** Transform an ordered pixel trace into approximate WGS 84 geometry.

**Tasks:**

- Apply the saved image calibration transform.
- Convert through the map projection to WGS 84.
- Simplify/resample geometry for later matching.
- Display the approximate line independently from routed geometry.

**Acceptance criteria:**

- Known fixture pixels map to expected coordinates within defined tolerances.
- The approximate line follows the calibrated image.
- Calibration errors can be distinguished from map-matching errors.

## PBI-160 — Bicycle-network map matching

**Status:** Not started

**Goal:** Match an approximate directed trace to a legal bicycle network.

**Tasks:**

- Define a provider-neutral map-matching request/result.
- Integrate one map-matching implementation.
- Request cycling-aware costing.
- Retain confidence or diagnostic information when the provider supplies it.
- Preserve the original approximate line for comparison.

**Acceptance criteria:**

- Representative traces snap to connected rideable geometry.
- Direction and access restrictions are respected as far as the provider
  supports them.
- Unmatched portions are visible.
- Provider behaviour and limitations are documented.

## PBI-170 — Ambiguity and correction workflow

**Status:** Not started

**Goal:** Allow the user to inspect and repair uncertain or incorrect matches.

**Tasks:**

- Highlight low-confidence, unmatched, or unusually divergent sections.
- Allow adding a forced via point.
- Allow selecting an alternative or manually replacing a section.
- Re-run only the affected match where practical.
- Keep an undoable edit history.

**Acceptance criteria:**

- The user can repair each failure represented in the fixture set.
- Automatic reruns do not discard manual corrections without warning.
- Export uses the reviewed geometry.

## PBI-180 — Elevation enrichment

**Status:** Not started

**Goal:** Add optional elevation and an elevation profile to planned routes.

**Tasks:**

- Define an elevation-provider boundary.
- Sample elevation at an appropriate interval.
- Avoid excessive API requests.
- Show source and missing-data state.
- Recalculate ascent/descent using documented smoothing rules.

**Acceptance criteria:**

- Enrichment is optional and does not block GPX export.
- Existing elevation is not silently overwritten.
- Unit and smoothing rules are tested and documented.

## PBI-190 — POIs, custom waypoints, and cue model

**Status:** Not started

**Goal:** Add portable points of interest and establish a provider-neutral cue
model.

**Tasks:**

- Create/edit named waypoints with description and type.
- Associate waypoints with the route without confusing them with geometry
  points.
- Model manoeuvres/cues independently from GPX extensions.
- Determine what can be represented portably in standard GPX.

**Acceptance criteria:**

- Waypoints round-trip through GPX.
- Unsupported device-specific semantics are identified.
- Route geometry remains valid without cues.

## PBI-200 — Course formats and device compatibility

**Status:** Not started

**Goal:** Evaluate richer navigation exports and maintain real-device
compatibility evidence.

**Tasks:**

- Evaluate TCX Course and FIT Course output.
- Map the internal cue/POI model to supported fields.
- Create representative GPX/TCX/FIT fixtures.
- Test imports through Wahoo and at least one Garmin workflow.
- Document which device generates turns and which cues are embedded.

**Acceptance criteria:**

- Standard GPX remains available.
- Any added format has automated structural validation.
- Manual compatibility results include device/service, firmware/app version,
  date, and observed behaviour.
- Do not claim universal compatibility from a single successful import.

## PBI-210 — Development orchestration and telemetry

**Status:** Not started

**Goal:** Add optional Aspire-based local orchestration and browser telemetry
without changing the statically hosted production application.

**Tasks:**

- Add an Aspire AppHost for local development.
- Evaluate the Aspire Blazor hosting integration and development Blazor Gateway
  for the pinned .NET SDK.
- Pin and document a compatible Aspire version and record whether the required
  integration remains preview.
- Forward browser OpenTelemetry through a development-only, same-origin
  endpoint to the local Aspire dashboard.
- Document local startup and a browser-telemetry smoke check.

**Acceptance criteria:**

- The AppHost runs the Web project and exposes the Aspire dashboard.
- A documented smoke check demonstrates browser telemetry in the dashboard.
- A normal Web project publish still produces static assets that run without
  AppHost, the gateway, or a telemetry backend.
- No dashboard credentials or telemetry ingestion secrets are exposed to the
  browser.
- No production server or production telemetry dependency is introduced.

## PBI-220 — Azure Static Web Apps infrastructure

**Status:** Not started

**Goal:** Define the optional Azure hosting resources as repeatable
infrastructure without changing the static application architecture.

**Tasks:**

- Define Azure Static Web Apps and required configuration using Bicep.
- Parameterise resource names, location, and environment-specific values.
- Configure client-side routing, static asset behaviour, and safe application
  settings.
- Document provisioning and removal.

**Acceptance criteria:**

- A clean Azure environment can be provisioned from source-controlled IaC.
- The infrastructure introduces no production application server.
- No secret is committed or embedded in the browser application.
- Provisioning remains independent of optional Aspire development tooling.

## PBI-230 — Automated static deployment

**Status:** Not started

**Goal:** Deploy a verified static release through CI after the core product is
useful.

**Tasks:**

- Build, test, and publish the AOT WebAssembly application before deployment.
- Deploy the published `wwwroot` output to the provisioned Static Web App.
- Prefer short-lived or platform-managed CI credentials where supported.
- Add a production smoke check and document recovery from a failed deployment.

**Acceptance criteria:**

- Deployment occurs only after all required checks pass.
- The hosted application loads directly and through a client-side route.
- GPX import and export pass a production smoke check without a backend.
- Deployment configuration and operational secrets remain outside client
  assets.
