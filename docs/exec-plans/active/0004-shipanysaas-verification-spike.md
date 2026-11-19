# Execution plan 0004 — ShipAnySaaS verification spike

Status: Not started (can run in parallel with 0002/0003) | Last reviewed: 2026-11-19

## Goal
Decide, with evidence, whether and how ShipAnySaaS provides identity, team tenancy, billing and notifications for the ASP.NET Core product.

## Scope
- In: licence review, documentation and source review under a valid licence, a throwaway proof of token validation and webhook handling outside this repository.
- Out: production integration; copying template source into this repository.

## Assumptions
- "ShipAnySaaS" = ShipAny (confirm first).
- Public docs (accessed 2026-11-19) describe TypeScript source templates with better-auth and Stripe.

## Tasks
- [ ] Confirm product identity and licence terms with the owner.
- [ ] Complete checklist 1–8 in [shipanysaas-integration.md](../../design-docs/shipanysaas-integration.md) with evidence.
- [ ] Compare options A/B/C on security, cost and operations.
- [ ] Log decision (new D-entry; supersede or accept ADR-0012); update TD-004.

## Acceptance criteria
- Every checklist item marked pass/fail with evidence links.
- Selected option documented; web plan 0005 unblocked or re-scoped.

## Risks
- Licence prohibits intended use → option B.
- Spike code leaks into product → spike lives outside the repo and is discarded.

## Rollback
Decision-only; no product changes.

## Related
[ADR-0012](../../design-docs/adr/ADR-0012-shipanysaas-behind-ports.md) · [web-and-tenancy.md](../../design-docs/web-and-tenancy.md) · [threat-model.md](../../design-docs/threat-model.md)
