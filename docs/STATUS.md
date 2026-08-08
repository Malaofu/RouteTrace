# Project status

**Phase:** Build reliability before workspace development

**Current PBI:** PBI-061 — Restore Release AOT CI

**Last updated:** 2026-08-08

## Current state

- PBI-000 through PBI-060 are complete.
- GPX 1.1 import, inspection, map visualisation, export, schema validation, and
  semantic round-trip coverage are implemented.
- The last verified local baseline was 47 passing .NET tests and three
  Playwright performance tests.
- Release WebAssembly builds use AOT compilation.

## Blocker

- CI fails during the Release build because the clean runner does not have the
  `wasm-tools` workload installed.

## Next action

- Complete PBI-061 by installing the workload in CI and verifying the existing
  Release build, tests, publish, and AOT performance checks.

## Deferred choices

- Production map-data, routing, and map-matching providers.
- Image-processing implementation.
- Optional Aspire development telemetry in PBI-210.
- Azure deployment in PBI-220 and PBI-230.
