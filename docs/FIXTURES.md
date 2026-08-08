# Test fixtures

This document tells the user what files or real-device evidence must be
acquired before a PBI depends on them. It also prevents tests from silently
substituting synthetic data for exporter- or device-specific behaviour.

## Mandatory fixture check

At the beginning of every implementation task, Codex reports one of:

- `No fixtures needed for PBI-NNN.`
- `All fixtures for PBI-NNN are present: ...`
- `Fixtures needed for PBI-NNN: ...`

For each missing item, the report includes its fixture ID, purpose, required
characteristics, accepted file types, whether a synthetic substitute is
acceptable, and required sanitisation. Fixture-dependent implementation pauses
until the user supplies it or explicitly approves a substitute.

Store small, sanitised fixtures under `tests/RouteTrace.TestData/`. Never commit
home locations, personal movement history, account identifiers, tokens, or
copyrighted map imagery without permission. Record provenance and expected
behaviour in `tests/RouteTrace.TestData/README.md`.

## Fixture catalogue

| ID           | Description                                                                        | Source and handling |
|--------------|------------------------------------------------------------------------------------|---|
| FX-GPX-001   | Minimal GPX 1.1 track with elevation and timestamps                                | Synthetic is acceptable. |
| FX-GPX-002   | Real GPX exported from a common cycling service or device                          | User-supplied and sanitised; retain only the fields needed by the test. |
| FX-GPX-002-a | Same source as FX-GPX-002 with realistic full-density point count                  | User-supplied and sanitised; retain the interior point density needed by the performance test. |
| FX-GPX-003   | Multiple tracks and segments, including a deliberate segment gap                   | Synthetic is acceptable. |
| FX-GPX-004   | Routes, waypoints, metadata, and at least one vendor extension namespace           | Prefer a user-supplied sanitised export; synthetic extension XML may supplement it. |
| FX-GPX-005   | Synthetic GPX 1.1 document populating the complete standard schema surface          | Synthetic, generated from the official GPX 1.1 XSD with user approval. |
| FX-GPX-006   | Tracks/routes with open and loop geometry plus waypoints with known, unknown, and missing `sym` values | Synthetic is acceptable; optionally supplement with a sanitised Wahoo or Garmin export. |
| FX-IMG-001   | Clean digital route-map image with a distinct route colour                         | User-supplied or purpose-created; PNG/JPEG/WebP. |
| FX-IMG-002   | Route image containing labels, markers, crossings, and anti-aliased edges          | User-supplied or purpose-created; PNG/JPEG/WebP. |
| FX-IMG-003   | Rotated or perspective-distorted photograph/scan of a route                        | User-supplied; PNG/JPEG/WebP. |
| FX-PAIR-001  | A route image and reference GPX representing the same route                        | User-supplied or purpose-created; sanitise both consistently. |
| FX-MATCH-001 | Approximate traces for known ambiguous, disconnected, restricted, or one-way areas | User identifies non-sensitive test areas; coordinate JSON or GPX is acceptable. |
| FX-ELE-001   | Equivalent route samples with complete, partial, and absent elevation              | Synthetic is acceptable. |
| FX-DEVICE-001 | Files tested through Wahoo and Garmin import workflows, plus observed results      | User/friend supplies manual evidence; remove account data and note device/app versions. |

## PBI requirements

| PBI             | Required fixtures                              | When to ask |
|-----------------|------------------------------------------------|---|
| PBI-000–PBI-020 | None                                           | State that none are needed. |
| PBI-030         | FX-GPX-001, FX-GPX-002, FX-GPX-003, FX-GPX-004 | Ask before designing the final parser fixture suite. Synthetic work may begin for 001/003. |
| PBI-040         | Reuse PBI-030 GPX fixtures                     | Ask only if they are absent. |
| PBI-050         | Reuse PBI-030 plus FX-ELE-001                  | Ask before statistics tests. |
| PBI-051         | FX-GPX-002-a                                   | Ask before performance tests. |
| PBI-060         | Reuse PBI-030 GPX fixtures plus FX-GPX-005     | Ask only if they are absent. |
| PBI-061         | None                                           | State that none are needed. |
| PBI-070–PBI-074 | Reuse existing GPX fixtures                    | Ask only if they are absent; no new user-supplied files are required. |
| PBI-075         | FX-GPX-004, FX-GPX-006                         | Synthetic symbol coverage is sufficient; ask only if real-device symbol evidence is desired. |
| PBI-076         | FX-ELE-001                                     | Ask only if it is absent. |
| PBI-080–PBI-090 | None                                           | State that none are needed. Real routing locations may be suggested but are not file fixtures. |
| PBI-100–PBI-110 | FX-IMG-001, FX-IMG-002, FX-IMG-003             | Ask before image validation/calibration tests. |
| PBI-120         | FX-PAIR-001                                    | Ask before acceptance testing of the complete tracing workflow. |
| PBI-130–PBI-150 | FX-IMG-001, FX-IMG-002, FX-PAIR-001            | Ask before choosing tolerances or acceptance thresholds. |
| PBI-160–PBI-170 | FX-PAIR-001, FX-MATCH-001                      | Ask before provider evaluation and correction tests. |
| PBI-180         | FX-ELE-001                                     | Ask before enrichment and calculation tests. |
| PBI-190         | FX-GPX-004                                     | Ask only if it is absent. |
| PBI-200         | FX-GPX-004, FX-DEVICE-001                      | Ask early; real-device verification may take more than one weekend. |
| PBI-210         | None                                           | State that none are needed. |
| PBI-220–PBI-230 | None                                           | State that none are needed. Azure access is an implementation prerequisite, not a committed test fixture. |

## Fixture request template

```text
Fixtures needed for PBI-NNN:

- FX-... — purpose
  - Supply: exact file types and required characteristics
  - Sanitise: data to remove or alter
  - Substitute: allowed or not allowed
  - Needed before: affected task or acceptance criterion
```
