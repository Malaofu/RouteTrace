# Roadmap

The roadmap is ordered by usable capability. PBI identifiers refer to
`BACKLOG.md`.

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
- PBI-070: Browser-local project persistence.

Outcome: a useful standalone GPX viewer and basic project workspace.

## Phase 2 — Manual route creation

- PBI-080: Manual freehand/point route editing.
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

## Later development infrastructure

- PBI-210: Optional Aspire development orchestration and telemetry.

This work is deliberately outside the product delivery path and can be
scheduled when local observability becomes useful.

## Release markers

- **Viewer:** PBI-070 complete.
- **Manual planner:** PBI-090 complete.
- **Image-assisted MVP:** PBI-120 complete.
- **Automatic-tracing beta:** PBI-170 complete.
- **Navigation export release:** PBI-200 complete.
