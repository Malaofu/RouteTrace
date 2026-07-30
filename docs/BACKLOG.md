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

**Status:** Not started

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

**Status:** Not started

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

**Status:** Not started

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

## PBI-060 — GPX export and round trip

**Status:** Not started

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

## PBI-070 — Browser-local project persistence

**Status:** Not started

**Goal:** Save and reopen unfinished projects without login or cloud storage.

**Tasks:**

- Define a versioned project-storage DTO.
- Store project data in IndexedDB.
- List, name, reopen, and delete local projects.
- Handle incompatible/corrupt saved data without breaking the application.

**Acceptance criteria:**

- A project survives browser refresh and application restart.
- Project deletion requires deliberate user action.
- Storage schema version is explicit.
- Images are not yet persisted unless required by this PBI's implementation.

## PBI-080 — Manual route editing

**Status:** Not started

**Goal:** Allow a user to create and adjust an unrouted line on the map.

**Tasks:**

- Add, move, insert, and delete ordered editing points.
- Choose or reverse direction.
- Clear or undo recent edits.
- Convert the edited line into canonical track geometry.

**Acceptance criteria:**

- The complete edit workflow works without a routing provider.
- Direction is visible.
- Undo covers the operations introduced by this PBI.
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
