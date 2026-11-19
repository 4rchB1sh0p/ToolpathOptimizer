# Frontend architecture

Status: Proposed — shell choice blocked on plan 0004 | Last reviewed: 2026-11-19

## Purpose and scope
- Web UI structure and rules. UI technology for the shell depends on the identity/billing option chosen in [shipanysaas-integration.md](design-docs/shipanysaas-integration.md).

## Key points
- Pages: projects, import wizard, constraints, analysis comparison, report history, settings.
- The ASP.NET Core API is the source of truth for validation and metrics; the UI never computes authoritative values.
- All actions are tenant-scoped server-side.
- Large point sets rendered downsampled; data table stays complete.
- Accessibility: keyboard operation, labelled axes, text tables, contrast, reduced motion.
- Strict Content-Security-Policy; no inline scripts; user-supplied text always encoded.

## Acceptance criteria
- UI integration tests show server errors (problem details) with codes and fixes.
- Accessibility checks pass on core flows.

## Risks
- Two-stack UI if ShipAny shell is chosen → keep visualiser embeddable and framework-light.

## Related
[DESIGN.md](DESIGN.md) · [visualization.md](design-docs/visualization.md) · [web-and-tenancy.md](design-docs/web-and-tenancy.md)
