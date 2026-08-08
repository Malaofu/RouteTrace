# Project status

**Phase:** Build reliability before workspace development

**Current PBI:** PBI-061 — Restore Release AOT CI

**Last updated:** 2026-08-08

## Current state

- PBI-000 through PBI-061 are complete.
- GPX 1.1 import, inspection, map visualisation, export, schema validation, and
  semantic round-trip coverage are implemented.
- CI installs the `wasm-tools` workload before its Release build and publish.
- Release WebAssembly builds and publishes remain AOT compiled.

## Verification

- Release build: passed with zero warnings.
- .NET tests: 47 passed, zero failed.
- Release AOT publish: passed; 48 assemblies AOT compiled.
- Playwright performance tests: 3 passed, zero failed.
- Formatting check: passed.

## Blockers

- None.

## Next action

- Select the next PBI explicitly; `CURRENT_PBI.md` remains PBI-061.

## Deferred choices

- Production map-data, routing, and map-matching providers.
- Image-processing implementation.
- Optional Aspire development telemetry in PBI-210.
- Azure deployment in PBI-220 and PBI-230.
