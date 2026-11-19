# ADR-0014 — IEC 62443-inspired security architecture without certification claims

Status: Proposed | Date: 2026-11-19 | Log: D-015

## Context
Industrial buyers use IEC 62443 vocabulary. The product is IT software that handles engineering data and never connects to OT.

## Decision
- Use zones and conduits, the seven foundational requirements (FR1–FR7) and secure-development practices as **design inspiration only**.
- No conduit to shop-floor OT; data leaves only as human-reviewed reports.
- Do not assign or claim a security level (SL), conformance, compliance or certification.
- Mapping and controls: [SECURITY.md](../../SECURITY.md); threats: [threat-model.md](../threat-model.md).

## Consequences
- Security requirements are traceable to FR categories for buyer questionnaires.

## Alternatives rejected
- Claim alignment: requires assessed evidence and licensed standard editions.

## Acceptance criteria
- No document or UI text claims IEC 62443 compliance/certification (claims grep in review).
- Each FR has at least one documented control or explicit "not applicable" reason.

## Risks
- Buyers read "inspired" as "compliant" → state the limitation in sales material.
- Standard editions change → verify current editions before external use.

## Related
[SECURITY.md](../../SECURITY.md) · [threat-model.md](../threat-model.md) · [engineering-standards.md](../engineering-standards.md)
