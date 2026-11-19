# Threat model

Status: Proposed (pre-implementation) | Method: STRIDE + zones/conduits | Last reviewed: 2026-11-19

## Purpose and scope
- Identifies assets, trust boundaries, threats and mitigations for CLI, Web, storage and identity/billing integration.
- Out of scope: shop-floor OT networks and controllers (no conduit exists by design).

## Assets
Customer drill geometry and metadata (confidential IP) · reports and audit trail · tenant/user identities · credentials and signing keys · algorithm integrity (correct, unmodified results) · service availability.

## Zones and conduits (IEC 62443-inspired, no SL claim)
| Zone | Contents | Trust |
|---|---|---|
| Z1 Engineering workstation | CLI, local input/report files | User-controlled |
| Z2 SaaS application | Web API/UI, worker (later) | Operator-controlled |
| Z3 Data | Database, object storage | Operator-controlled, private network |
| Z4 External providers | Identity/billing/email (provider TBD) | Third party |
| Z5 Shop-floor OT | CNC controllers, DNC | **Outside system; no conduit** |

Conduits: C1 browser ↔ Z2 (HTTPS) · C2 Z2 ↔ Z3 (private, authenticated) · C3 Z2 ↔ Z4 (HTTPS, signed webhooks) · human-reviewed report transfer to CAM only.

## Threats and mitigations
| ID | STRIDE | Threat | Mitigation | Verification |
|---|---|---|---|---|
| T1 | Spoofing | Forged tenant ID in body/route | Tenant only from validated credential | Spoofing integration tests |
| T2 | Elevation | Cross-tenant access (IDOR) | Tenant filter on every query/key; 404 on mismatch | Per-endpoint tests |
| T3 | DoS | Huge/deep files, many points, pathological precedence graphs | Size, row, depth, edge limits before full parse; deadlines; per-tenant rate limits | Limit tests, fuzzing |
| T4 | Tampering | Malformed CSV/JSON exploiting parser | Strict hand-written CSV reader, strict JSON, no reflection-based polymorphism | Fuzz + negative tests |
| T5 | Tampering | Formula injection in exported CSV | ID pattern + escaping of leading `= + - @` | Export tests |
| T6 | Info disclosure | Geometry/IDs in logs or errors | Data-minimised logging; truncated messages | Log capture tests |
| T7 | Tampering | Silent wrong result (optimiser bug) presented as valid | Independent validator; fail closed (exit 5) | Corrupted-route tests |
| T8 | Repudiation | Disputed who ran/approved an analysis | Immutable runs, audit events with actor/time | Audit tests |
| T9 | Spoofing | Forged billing webhook | Signature + timestamp + replay check | Webhook tests |
| T10 | Tampering | CLI path traversal / overwriting files | Explicit output path, no overwrite by default, temp-file in target dir | CLI tests |
| T11 | Elevation | Supply-chain compromise (NuGet, base images) | Lock files, source mapping, SBOM, scanning, pinned digests, signed releases | CI evidence |
| T12 | Info disclosure | Prompt injection/data leak via future AI | AI off by default; free text excluded; consent | Design review |
| T13 | Safety misuse | Report treated as verified NC sequence | Advisory notice; no NC output; wording review | UX review |
| T14 | Info disclosure | Secrets in repo/config | Secret store; scanning; `.env.example` only later | Secret scan |

## Acceptance criteria
- Each threat has a mitigation linked to a test or review artifact before the related plan closes.
- Threat model reviewed when a zone, conduit, provider or contract changes.

## Risks
- Provider choice (Z4) unknown → re-run T1, T2, T9 after plan 0004.
- Residual: insider misuse within a tenant; compromised user endpoint (Z1) — out of our control; documented for customers.

## Related
[SECURITY.md](../SECURITY.md) · [ADR-0014](adr/ADR-0014-iec62443-inspired-security.md) · [web-and-tenancy.md](web-and-tenancy.md) · [input-output-contracts.md](input-output-contracts.md)
