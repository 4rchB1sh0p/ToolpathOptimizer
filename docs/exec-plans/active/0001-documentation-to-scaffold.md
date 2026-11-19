# Execution plan 0001 — Documentation and discovery to scaffold

Status: Active (documentation phase) | Last reviewed: 2026-11-19

## Goal
Validate the drilling-order use case and approve the documentation baseline so the first code slice can start.

## Scope
- In: interviews, schema and boundary review, ADR review, scaffold readiness.
- Out: any code, Dockerfiles or Compose YAML.

## Assumptions
- Synthetic data only; no employer or customer files.
- "ShipAnySaaS" = ShipAny (to confirm in plan 0004).

## Tasks
- [x] Documentation baseline: ADRs, Decisions.md, specs, threat model, testing strategy, plans 0002–0006.
- [ ] Interview 5 CNC programmers; record anonymised workflow and constraints.
- [ ] Review and approve input/output contracts and the advisory boundary.
- [ ] Owners review Proposed ADRs (0002, 0007–0014) and set status.
- [x] Resolve TD-008 (unrelated `.vscode/` configuration) — D-027.
- [ ] Resolve TD-010 (ARCHITECTURE.md test project line).
- [x] Confirm .NET 10 LTS and tool choices for scaffold — D-019 to D-022; scaffold built.

## Acceptance criteria
- Interview evidence recorded; go/no-go decision logged as a D-entry.
- Contracts approved; ADR statuses updated.
- Every doc passes link and section checks.

## Risks
- No buyer interest → stop before code.
- Docs drift from reality once code starts → docs updated in each slice.

## Rollback
Documentation-only; revert edits. No runtime impact.

## Related
[PLANS.md](../../PLANS.md) · [Decisions.md](../../Decisions.md) · [PRODUCT_SENSE.md](../../PRODUCT_SENSE.md) · [0002 plan](0002-core-optimisation-slice.md)
