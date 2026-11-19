# New-user onboarding

Status: Proposed — identity steps depend on plan 0004 | Last reviewed: 2026-11-19

## Purpose and scope
- First-run web flow to a first advisory report using synthetic data only.

## Flow
1. Sign in and create or join a team (provider per [shipanysaas-integration.md](../design-docs/shipanysaas-integration.md)).
2. Create a project and select `2D independent drilling`.
3. Download the synthetic sample CSV/manifest.
4. Import; inspect validation, units and original route.
5. Run analysis with defaults; compare distance and route visually.
6. Download the advisory report; optionally invite a reviewer.

## Acceptance criteria
- Demo completes without uploading proprietary files.
- The decision-support (not CNC verification, not NC generation) message appears before first run and on the report.

## Risks
- Users skip the notice → repeat it on report and export.

## Related
[DESIGN.md](../DESIGN.md) · [sample-input.md](../references/sample-input.md) · [projects-and-reports.md](projects-and-reports.md)
