# Web architecture and tenancy

Status: Proposed — production identity blocked on plan 0004 | Last reviewed: 2026-11-19

## Purpose and scope
- ASP.NET Core API and UI host for projects, imports, analyses and reports; tenant isolation rules.
- Out of scope: identity/billing provider choice ([shipanysaas-integration.md](shipanysaas-integration.md)); OT connectivity (never).

## API sketch (v1, versioned path)
| Method and path | Purpose |
|---|---|
| `POST /api/v1/projects` | Create project |
| `POST /api/v1/projects/{projectId}/inputs` | Upload CSV + manifest (multipart) → immutable input artifact |
| `POST /api/v1/projects/{projectId}/analyses` | Run analysis (sync, small jobs); `Idempotency-Key` header |
| `GET /api/v1/projects/{projectId}/analyses/{runId}` | Status and summary |
| `GET /api/v1/projects/{projectId}/analyses/{runId}/report` | Report JSON / CSV summary |
| `DELETE /api/v1/projects/{projectId}` · `GET …/export` | Deletion and data export |

## Role permissions (same tenant)
| Action | Owner | Engineer | Reviewer | Viewer |
|---|---|---|---|---|
| Read projects, runs, reports | ✓ | ✓ | ✓ | ✓ |
| Create project, upload input, run analysis | ✓ | ✓ | — | — |
| Annotate / mark "Reviewed (advisory)" | ✓ | ✓ | ✓ | — |
| Delete or export project | ✓ | — | — | — |
| View audit events | ✓ | — | — | — |

- Membership and role management stay with the identity provider; the API maps provider roles to these four.
- Denied same-tenant action → 403; cross-tenant resource → 404.

## Rules
- Tenant ID comes only from the validated credential (`ITenantContext`); never from route, query or body.
- Every query and storage key filters by tenant; cross-tenant access returns 404.
- Errors as RFC 9457 problem details with stable `code` (same codes as CLI).
- Request size limits, multipart limits, rate limiting per tenant and per user, antiforgery for cookie-authenticated forms.
- Server recomputes all metrics; browser visualisation is advisory display only.
- Inputs and runs are immutable; rerunning with a new algorithm version creates a new run.
- Start synchronous with a hard deadline; durable queue + worker when TD-003 triggers.
- Quotas by points, file size and runs per period, enforced through `IEntitlementService`.

## Acceptance criteria
- Integration tests (`WebApplicationFactory`) prove: cross-tenant read/write denied; tenant spoofing via body/route ignored; limits return 413/429 problem details.
- Role matrix tests: every action denied for each role without the ✓ (e.g. Viewer upload/delete → 403).
- Same input via Web and CLI yields identical report bytes (excluding timestamp).
- Non-Development start without a verified identity adapter fails.

## Risks
- Identity provider mismatch (TD-004) → ports-first design.
- Long jobs time out (TD-003) → measure, then queue.
- IDOR bugs → mandatory tenant-filter tests per endpoint.

## Related
[ADR-0012](adr/ADR-0012-shipanysaas-behind-ports.md) · [FRONTEND.md](../FRONTEND.md) · [threat-model.md](threat-model.md) · [db-schema.md](../generated/db-schema.md) · [0005 plan](../exec-plans/active/0005-web-api-and-tenancy.md)
