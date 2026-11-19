# MVP product specification

Status: Proposed | Last reviewed: 2026-11-19

## Purpose and scope
- Smallest product that lets a CNC programmer compare a drilling order with a constraint-respecting alternative and export a reviewable report.
- Surfaces: offline CLI first ([offline-cli-analysis.md](offline-cli-analysis.md)), then web ([projects-and-reports.md](projects-and-reports.md)).

## User stories
1. Import neutral CSV and manifest; receive precise validation errors.
2. Preview point geometry and baseline path.
3. Set fixed start/end, precedence and locked groups.
4. Run a deterministic optimisation; see distance delta, feasibility and warnings.
5. Export a JSON (and optional CSV summary) report; original order preserved.

## Acceptance criteria
- No reordering across precedence or locked groups; every candidate passes the independent validator.
- Invalid inputs rejected with error code, location and fix; no silent approximation.
- Same input, settings and algorithm version → same route and distances.
- Baseline selected when the candidate is not shorter than a feasible baseline.
- All metrics independently reproducible from the report.
- No NC output; every result marked advisory.

## Out of scope
3D toolpaths, milling contours, collisions, machine kinematics, controller simulation, AI-generated G-code, live machine data, vendor post-processors, time/cost estimates.

## Risks
- Strict input rejects real exports → interview-driven format review (plan 0001).
- Distance gains too small to matter → measure on synthetic and pilot data before claims.

## Related
[PRODUCT_SENSE.md](../PRODUCT_SENSE.md) · [geometry-and-optimization.md](../design-docs/geometry-and-optimization.md) · [input-output-contracts.md](../design-docs/input-output-contracts.md) · [ADR-0003](../design-docs/adr/ADR-0003-advisory-only-boundary.md)
