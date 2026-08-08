# Roadmap

The roadmap is ordered by usable capability. PBI identifiers refer to
`BACKLOG.md`; an optional PBI does not block the following product phase.

## Immediate maintenance

- PBI-061: Restore Release AOT CI.

Outcome: the existing product baseline builds, tests, and publishes on a clean
runner before new feature work begins.

## Phase 0 — Foundation

- PBI-000: Repository and static application baseline.
- PBI-010: Interactive map shell and attribution.

Outcome: an empty but deployable mapping application.

## Phase 1 — GPX viewer

- PBI-020: Canonical route model.
- PBI-030: GPX import and validation.
- PBI-040: Route, segment, and waypoint visualisation.
- PBI-050: GPX inspection and derived statistics.
- PBI-060: GPX export and round-trip fixtures.
- PBI-070: Browser-local multi-document workspace persistence.

Outcome: a useful standalone GPX viewer with durable local workspaces.

## Phase 1.5 — Workspace and interaction shell

- PBI-071: Central application menu and command surface.
- PBI-072: Multiple open GPX documents.
- PBI-073: Hierarchical GPX document explorer.
- PBI-074: Context actions and presentation settings.
- PBI-075: Route endpoint and POI symbols.
- PBI-076: Existing-elevation profile (optional).

Outcome: a scalable editor shell inspired by established GPX tools without yet
introducing route-geometry editing.

## Phase 2 — Manual route creation

- PBI-080: Manual freehand/point route editing and shared undo/redo.
- PBI-090: Bicycle routing between ordered anchors.

Outcome: a simple cycling route planner independent of image recognition.

## Phase 3 — Image-assisted tracing

- PBI-100: Local image import and overlay.
- PBI-110: Image placement and geographic calibration.
- PBI-120: Manual routed tracing over the image.

Outcome: the first product-specific useful release. A route image can be
recreated using relatively few clicks.

## Phase 4 — Automatic extraction

- PBI-130: Route-colour selection and segmentation.
- PBI-140: Centreline extraction and ordered pixel trace.
- PBI-150: Convert extracted pixels into geographic geometry.
- PBI-160: Bicycle-network map matching.
- PBI-170: Ambiguity and correction workflow.

Outcome: semi-automatic image-to-route conversion with explicit user review.

## Phase 5 — Navigation enrichment

- PBI-180: Elevation enrichment.
- PBI-190: POIs, custom waypoints, and cue modelling.
- PBI-200: TCX/FIT evaluation and device compatibility suite.

Outcome: richer files and tested interoperability with common cycling devices.

## Optional development infrastructure

- PBI-210: Aspire development orchestration and telemetry.

This work can be scheduled whenever local observability becomes useful and does
not block product delivery or static deployment.

## Phase 6 — Static deployment

- PBI-220: Azure Static Web Apps infrastructure as code.
- PBI-230: Automated static deployment and production smoke checks.

Outcome: a repeatable hosted release without introducing a production
application server.

## Release markers

- **Workspace viewer:** PBI-075 complete; PBI-076 is optional.
- **Manual planner:** PBI-090 complete.
- **Image-assisted MVP:** PBI-120 complete.
- **Automatic-tracing beta:** PBI-170 complete.
- **Navigation export release:** PBI-200 complete.
- **Hosted release:** PBI-230 complete.
