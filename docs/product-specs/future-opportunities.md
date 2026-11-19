# Future opportunities (not MVP commitments)

Status: Backlog | Last reviewed: 2026-11-19

## Purpose and scope
- Ideas parked until customer validation; none are commitments.

## Candidates
Multi-tool drilling sequences · tool-change penalties · group-order constraints (TD-007) · Or-opt relocation of locked groups (TD-006) · inch input · 3D point-to-point positioning · acceleration/jerk-aware time models (TD-001) · obstacle clearance screening · DXF/STEP/CAM import (TD-002) · enterprise on-prem deployment.

**Outside the product boundary:** controller-specific post-processing or any machine-ready NC output. Considering it would require superseding [ADR-0003](../design-docs/adr/ADR-0003-advisory-only-boundary.md) and AGENTS.md rule 2; it is not a backlog item.

## Acceptance criteria (to promote an item)
- Validated customer need, new ADR, separate safety/verification analysis and updated threat model.

## Risks
- Scope creep toward full CAM → each item re-checked against [PRODUCT_SENSE.md](../PRODUCT_SENSE.md).
- Post-processing and clearance screening carry safety risk → clearance screening stays out until a verification approach exists; post-processing is excluded (see above).

## Related
[mvp.md](mvp.md) · [Decisions.md](../Decisions.md)
