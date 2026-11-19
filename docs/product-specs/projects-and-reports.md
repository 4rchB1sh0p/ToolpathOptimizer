# Projects and reports

Status: Proposed | Last reviewed: 2026-11-19

## Purpose and scope
- How teams store inputs, runs and reports in the web product.

## Key points
- Projects belong to a team (tenant) and contain immutable input versions and analysis runs.
- Each report shows original/candidate order, selected route, constraints, units, geometry preview, algorithm/version/parameters, distance comparison, warnings, initiator and time.
- Users may annotate or mark "approved for internal review"; this never means machine-safety certification.
- Deletion and export per project; access audit trail.

## Acceptance criteria
- Report content matches [input-output-contracts.md](../design-docs/input-output-contracts.md) report v1.
- Annotations cannot alter inputs, runs or metrics.
- Deletion removes inputs, reports and derived artifacts within the documented retention window.

## Risks
- "Approved" misread as "safe" → label "Reviewed (advisory)".
- Retention undefined → blocked until defined (RELIABILITY).

## Related
[web-and-tenancy.md](../design-docs/web-and-tenancy.md) · [db-schema.md](../generated/db-schema.md) · [RELIABILITY.md](../RELIABILITY.md)
