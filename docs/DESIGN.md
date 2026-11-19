# Product and interaction design

Status: Proposed | Last reviewed: 2026-11-19

## Purpose and scope
- Interaction principles for engineers reviewing a proposed drill sequence (not machine operators).

## Key points
- Show together: input assumptions, units, geometry preview, baseline and candidate routes, distance delta, constraint status, warnings, export.
- Every result screen and export shows the advisory notice.
- Keyboard-accessible controls, readable charts, non-colour status indicators ([visualization.md](design-docs/visualization.md)).
- **Forbidden claims** (UI, reports, marketing): "safe", "collision-free", "verified for CNC", "machine-ready", "optimal", "certified/compliant", time or cost savings without measured data.
- Preferred wording: "advisory", "shorter rapid travel distance (estimate)", "feasible under the stated constraints".

## Acceptance criteria
- Copy review against the forbidden-claims list before each release.
- Accessibility checks on comparison and import screens.

## Risks
- Engineers over-trust visuals → constraint status and notice beside every route.

## Related
[FRONTEND.md](FRONTEND.md) · [ADR-0003](design-docs/adr/ADR-0003-advisory-only-boundary.md) · [new-user-onboarding.md](product-specs/new-user-onboarding.md)
