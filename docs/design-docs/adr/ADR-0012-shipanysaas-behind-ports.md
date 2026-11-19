# ADR-0012 — ShipAnySaaS integration behind ports, gated by verification

Status: Proposed — blocked on plan 0004 | Date: 2026-11-19 | Log: D-012

## Context
- The brief names "ShipAnySaaS" for identity, teams, billing and notifications.
- Public ShipAny docs (shipany.ai/docs, accessed 2026-11-19) describe a paid family of **TypeScript source templates** (Next.js / TanStack Start) with better-auth, Stripe and other payment providers, credits, RBAC and CMS. No hosted API for third-party backends, team/organisation tenancy model or licence terms for our use were confirmed.
- Assumption to confirm: "ShipAnySaaS" means ShipAny.

## Decision
- Application defines ports: `ICurrentPrincipal`, `ITenantContext`, `IEntitlementService`, `IAuditLog`, `INotificationSender`.
- The ASP.NET Core API derives tenant only from a validated credential, never from route or body.
- No ShipAnySaaS adapter is built until [shipanysaas-integration.md](../shipanysaas-integration.md) checklist passes.
- Until then, Web runs only in `Development` with a fake principal that refuses to start in any other environment.

## Consequences
- Web slice can proceed with ports and fakes; production auth is blocked.

## Alternatives rejected
- Build on assumed APIs: high rework and security risk.
- Port ShipAny TypeScript code to C#: licence and maintenance risk.

## Acceptance criteria
- Verification spike evidence recorded; option selected via a new D-entry.
- Cross-tenant access tests pass with the chosen adapter.

## Risks
- ShipAny unsuitable as an identity provider for an external API → fallback: standard OIDC provider + direct billing provider.
- Two stacks (TypeScript + .NET) increase operations cost.

## Related
[shipanysaas-integration.md](../shipanysaas-integration.md) · [web-and-tenancy.md](../web-and-tenancy.md) · [0004 plan](../../exec-plans/active/0004-shipanysaas-verification-spike.md)
