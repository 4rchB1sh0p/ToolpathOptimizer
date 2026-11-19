# ADR-0003 — Advisory-only output boundary

Status: Accepted (product boundary; AGENTS.md rules 1–2) | Date: 2026-11-19 | Log: D-003

## Context
The product has no machine kinematics, fixture, clamp, tool-length or collision model. Any executable output could be run without review.

## Decision
- Outputs are reports (JSON, optional CSV summary, visual comparison) only.
- Never emit G-code, NC programs, post-processed output or controller files.
- Every report carries a fixed advisory notice.
- Never label a route "safe", "verified", "collision-free" or "optimal"; never claim cycle-time or cost savings without measured shop data.

## Consequences
- A CAM programmer re-enters any accepted sequence in their CAM system and verifies it there.
- Distance deltas are presented as non-cutting travel distance only.

## Alternatives rejected
- NC export with disclaimers: disclaimers do not prevent unsafe execution.

## Acceptance criteria
- Output schema contains no field that encodes machine commands.
- UI and report text reviewed against the forbidden-claims list in [DESIGN.md](../../DESIGN.md).

## Risks
- Users still copy the order into NC unreviewed → onboarding and report notice state required CAM verification.
- Marketing overclaims → claims review before publication.

## Related
[ARCHITECTURE.md](../../../.github/ARCHITECTURE.md) · [mvp.md](../../product-specs/mvp.md) · [SECURITY.md](../../SECURITY.md)
