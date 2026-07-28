# Project definition

## Goal

Create a privacy-friendly web application that can turn a published image of a
cycling route into a route that can be inspected, corrected, exported, and
loaded on devices such as a Wahoo ELEMNT ROAM 3 or Garmin Edge.

The application should make image-derived routes substantially faster to
recreate than manually redrawing the complete route in an existing planner.
Human correction is an expected part of the workflow.

## Primary user journey

1. Open the application without creating an account.
2. Import an image containing a route.
3. Place or calibrate the image over a geographic map.
4. Extract or trace the route.
5. Match the approximate trace to a bicycle-routable road and path network.
6. Review ambiguous or illegal sections.
7. Export a portable route file.
8. Optionally import that file into RideWithGPS, Wahoo, Garmin, or another
   service.

## Supporting user journeys

- Import and inspect an existing GPX file.
- Display its tracks, routes, segments, waypoints, metadata, elevation, and
  timestamps where present.
- Make simple corrections to route geometry.
- Save unfinished work locally in the browser.
- Reopen and continue a locally saved project.

## Product principles

- **Static-first:** the UI must be deployable as static assets.
- **Local-first:** images, projects, and GPX processing remain on the client by
  default.
- **Observable development:** use Aspire orchestration and local telemetry from
  the first PBI without making production hosting depend on Aspire.
- **Progressive automation:** manual tracing must work before automatic image
  recognition exists.
- **User-controlled ambiguity:** uncertain map matches are shown for correction,
  not silently guessed.
- **Portable output:** standard GPX remains the baseline export.
- **Cycling-aware:** matching must eventually respect bicycle access,
  directionality, barriers, and route preferences.
- **Incremental delivery:** every PBI should leave the application usable or
  establish a tested prerequisite for the next visible feature.

## Initial scope

- Interactive world map with zoom and cycling-relevant detail.
- GPX import, display, inspection, and export.
- Browser-local project persistence.
- Image import, placement, calibration, and opacity controls.
- Manual route tracing over an image.
- Automatic extraction of a visually distinct route line.
- Bicycle-network matching with a correction workflow.
- Optional elevation enrichment and points of interest.
- Later evaluation of TCX and FIT Course output.

## Non-goals for the initial product

- User accounts or cross-device synchronisation.
- Social features or public route publishing.
- Multi-user collaborative editing.
- Native mobile applications.
- Full offline worldwide routing.
- Activity recording or fitness analytics.
- Training-plan or workout-file generation.
- Guaranteed fully automatic interpretation of every map style.
- High availability or production-scale infrastructure.

## Success criteria

The first meaningful release is successful when a user can:

1. Import and view an ordinary GPX track.
2. Overlay a route image on the correct map location.
3. trace the intended route using a small number of user-selected anchors,
4. obtain a bicycle-routed line between those anchors,
5. correct the result, and
6. export a GPX file accepted by the Wahoo application.

Automatic pixel-level route extraction is an enhancement after this workflow,
not a prerequisite for the first useful release.
