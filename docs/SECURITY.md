# Security and industrial boundary

Status: Proposed | Last reviewed: 2026-11-19

## Purpose and scope
- Security principles and controls for CLI, Web, storage and integrations.
- IEC 62443 is used as **design inspiration only**. No certification, compliance, conformance or security level is claimed ([ADR-0014](design-docs/adr/ADR-0014-iec62443-inspired-security.md)).

## Boundary
- No machine control, controller credentials, DNC links or inbound OT connections.
- Never execute uploaded files or generated content; never generate NC code.
- Data reaches the shop floor only through a human reviewing a report and re-entering the sequence in CAM.

## Foundational requirements (inspiration mapping)
| FR (IEC 62443) | Controls in this product |
|---|---|
| FR1 Identification and authentication | Verified provider tokens (issuer, audience, expiry, key rotation); MFA via provider; no shared accounts; CLI relies on OS user |
| FR2 Use control | Role/action matrix in [web-and-tenancy.md](design-docs/web-and-tenancy.md) (Owner, Engineer, Reviewer, Viewer); least privilege; server-side tenant and role authorisation on every request |
| FR3 System integrity | Strict input validation; independent feasibility validator; immutable inputs/runs with hashes; signed builds; SBOM |
| FR4 Data confidentiality | TLS; encryption at rest where supported; tenant isolation; data-minimised logs; deletion and export |
| FR5 Restricted data flow | Zones/conduits per [threat-model.md](design-docs/threat-model.md); private data network; CLI needs no network; no OT conduit |
| FR6 Timely response to events | Audit events; alerting; incident and vulnerability-disclosure procedure |
| FR7 Resource availability | Quotas, rate limits, deadlines, bounded parsing; backup and restore tests |

## Secure development practices (inspired by IEC 62443-4-1 themes)
Security requirements per plan · threat model per boundary change · analyzers and warnings-as-errors · fuzz/negative tests · dependency, secret and container scanning · defect triage with severity SLAs · documented security update process · user security guidance.

## Data and AI
- Minimise proprietary part data; support deletion/export; define retention before storing customer data.
- AI off by default; explicit consent and data-processing disclosure required ([ADR-0004](design-docs/adr/ADR-0004-deterministic-computation-optional-ai.md)).

## Acceptance criteria
- Each FR row has linked tests or review evidence before pilot (plan 0006).
- No text anywhere claims IEC 62443 certification/compliance or machine safety.
- Signed binaries and supply-chain checks before customer distribution.

## Risks
- Identity provider unverified (TD-004) → FR1/FR2 incomplete until plan 0004.
- Customers misread "inspired" as "compliant" → explicit statement in sales and onboarding.

## Related
[threat-model.md](design-docs/threat-model.md) · [engineering-standards.md](design-docs/engineering-standards.md) · [web-and-tenancy.md](design-docs/web-and-tenancy.md) · [Decisions.md](Decisions.md)
