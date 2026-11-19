# Execution plan 0005 — Web API and tenancy

Status: Blocked (needs 0003 and 0004) | Last reviewed: 2026-11-19

## Goal
Tenant-isolated ASP.NET Core API and UI for projects, imports, analyses and reports using the same Application handlers as the CLI.

## Scope
- In: Web project, identity/tenancy/entitlement adapters for the option chosen in 0004, persistence (provider decision, TD-005), visualiser, Web integration tests.
- Out: background worker (until TD-003 triggers), AI features.

## Assumptions
- Identity option selected and verified; database provider selected via D-entry.

## Tasks
- [ ] Endpoints per [web-and-tenancy.md](../../design-docs/web-and-tenancy.md); problem details; limits; rate limiting.
- [ ] Adapters for `ICurrentPrincipal`, `ITenantContext`, `IEntitlementService`, `IAuditLog`.
- [ ] Persistence and migrations; replace [db-schema.md](../../generated/db-schema.md) with generated schema.
- [ ] Visualiser and comparison page; accessibility checks.
- [ ] Cross-tenant, spoofing and limit tests; CLI/Web report parity test.

## Acceptance criteria
- Web criteria in [web-and-tenancy.md](../../design-docs/web-and-tenancy.md) met, including role-matrix tests.
- [QUALITY_SCORE.md](../../QUALITY_SCORE.md) rows applicable to this slice evidenced (geometry, constraints, determinism, regression, Web, Claims, UX, and Security excluding image scans). The full scorecard is required at plan 0006 closure.
- Threat-model items T1–T9 evidenced.

## Risks
- Two-stack complexity (option A) → strict API contract and shared problem codes.
- Synchronous jobs time out → measure; queue if needed.

## Rollback
Feature-flag new endpoints; database migrations reversible or forward-fixed with backups.

## Related
[ADR-0012](../../design-docs/adr/ADR-0012-shipanysaas-behind-ports.md) · [FRONTEND.md](../../FRONTEND.md) · [projects-and-reports.md](../../product-specs/projects-and-reports.md)
