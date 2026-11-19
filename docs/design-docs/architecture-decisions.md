# Architecture decision records (index)

## Purpose and scope
- Index and template for ADRs in [adr/](adr/). Former ADR-001…004 are now ADR-0001…0004 (same meaning, more detail).
- The short decision log and baseline tables live in [Decisions.md](../Decisions.md); ADRs hold the rationale.

## Index
| ADR | Title | Status |
|---|---|---|
| [0001](adr/ADR-0001-pure-deterministic-core.md) | Pure, deterministic Core library | Accepted |
| [0002](adr/ADR-0002-neutral-input-formats.md) | Neutral CSV + JSON manifest inputs only | Proposed |
| [0003](adr/ADR-0003-advisory-only-boundary.md) | Advisory-only output boundary | Accepted |
| [0004](adr/ADR-0004-deterministic-computation-optional-ai.md) | Deterministic computation; optional AI narrative | Accepted |
| [0005](adr/ADR-0005-layered-solution-structure.md) | Layered solution structure and project naming | Accepted |
| [0006](adr/ADR-0006-si-internal-units.md) | SI internal units with explicit boundary units | Accepted |
| [0007](adr/ADR-0007-nn-2opt-independent-validator.md) | NN + constrained 2-opt + independent validator | Proposed |
| [0008](adr/ADR-0008-deterministic-search-budgets.md) | Deterministic search budgets; deadline fails closed | Proposed |
| [0009](adr/ADR-0009-generic-host-and-cross-cutting.md) | Generic Host, DI and cross-cutting services | Proposed |
| [0010](adr/ADR-0010-versioned-strict-json-contracts.md) | Versioned strict JSON contracts and provenance | Proposed |
| [0011](adr/ADR-0011-stable-cli-exit-codes.md) | Stable CLI exit codes | Proposed |
| [0012](adr/ADR-0012-shipanysaas-behind-ports.md) | ShipAnySaaS behind ports, gated by verification | Proposed (blocked) |
| [0013](adr/ADR-0013-multi-stage-containers-deferred.md) | Multi-stage containers and Compose (deferred) | Proposed |
| [0014](adr/ADR-0014-iec62443-inspired-security.md) | IEC 62443-inspired security, no claims | Proposed |

"Accepted" here means restated from authoritative [AGENTS.md](../../.github/AGENTS.md)/[ARCHITECTURE.md](../../.github/ARCHITECTURE.md) or the product boundary; all others need owner review.

## ADR template
```text
# ADR-NNNN — <title>
Status: Proposed | Accepted | Superseded by ADR-NNNN | Date: YYYY-MM-DD | Log: D-NNN
## Context
## Decision
## Consequences
## Alternatives rejected
## Acceptance criteria
## Risks
## Related
```

## Acceptance criteria
- Every ADR has status, date, linked D-entry, acceptance criteria, risks and related links.
- Crossing a layer boundary or changing a public contract requires a new or superseding ADR.

## Risks
- ADR sprawl → one ADR per decision that is hard to reverse; minor choices go only in the D-log.

## Related
[Decisions.md](../Decisions.md) · [index.md](index.md)
