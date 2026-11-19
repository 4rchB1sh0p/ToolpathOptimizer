# ShipAnySaaS integration — verification gate

Status: Blocked — not verified | Last reviewed: 2026-11-19

## Purpose and scope
- Records what is known, what must be verified, and integration options for identity, teams/tenancy, billing and notifications.
- No integration may be built until every required check below passes ([ADR-0012](adr/ADR-0012-shipanysaas-behind-ports.md)).

## Known from public sources (not verified against source code or contracts)
- Source: shipany.ai/docs, accessed 2026-11-19.
- ShipAny is a paid membership family of **source-code templates** (Next.js App Router, TanStack Start, vinext; React 19; TypeScript).
- Templates pre-wire better-auth, Stripe (plus Alipay/WeChat Pay), credits, subscriptions, RBAC, i18n and CMS; deploy targets include Vercel, Cloudflare Workers and Docker.
- Assumption: "ShipAnySaaS" in the brief refers to ShipAny. **Confirm with the product owner.**

## Implication
It is not a hosted identity/billing API that an ASP.NET Core service can call directly. Integration means running a TypeScript app alongside the .NET API, or not using it.

## Options (none selected)
| Option | Shape | Main risk |
|---|---|---|
| A. ShipAny shell + .NET API | ShipAny app handles sign-in/billing UI; calls .NET API with a short-lived signed token validated via JWKS | Token/tenant claims support unverified; two stacks |
| B. .NET-only | ASP.NET Core with a standard OIDC provider and direct billing-provider integration | More UI/billing work; ShipAny unused |
| C. Split | ShipAny for marketing/billing site; product app separate with SSO | Two identity surfaces |

## Verification checklist (all required)
| # | Check | Pass condition |
|---|---|---|
| 1 | Licence permits our SaaS use, hosting and modification | Written terms reviewed |
| 2 | Identity can issue tokens for an external API | Signed JWT/OIDC with issuer, audience, expiry and JWKS rotation demonstrated |
| 3 | Team/organisation tenancy exists | Tenant ID claim stable; membership and roles queryable |
| 4 | Billing entitlements reachable by API | Signed webhooks or query API for plan/quota; replay protection |
| 5 | Notifications | Email sending contract and opt-out handling |
| 6 | Data residency, retention, deletion | Documented and testable |
| 7 | Security posture | Dependency update process, vulnerability disclosure, secrets handling |
| 8 | Operational fit | Deployment of a TypeScript app accepted by owners |

## Acceptance criteria
- Evidence for checks 1–8 stored in plan 0004; a new D-entry selects an option.
- Cross-tenant tests pass with the chosen adapter before production.

## Risks
- Template evolves (sync-upstream model) → pin versions; track changes.
- Vendor or licence change → keep ports provider-neutral.

## Related
[web-and-tenancy.md](web-and-tenancy.md) · [0004 plan](../exec-plans/active/0004-shipanysaas-verification-spike.md) · [Decisions.md](../Decisions.md) (TD-004)
