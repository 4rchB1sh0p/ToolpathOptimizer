# Core library design

Status: Proposed | Last reviewed: 2026-11-19

## Purpose and scope
- Type-level design of `ToolpathOptimizer.Core`: units, domain, validation, optimisation, verification.
- Out of scope: parsing, file I/O, hosting (Application/Infrastructure).

## Namespaces and key types
| Namespace | Types (sealed, immutable unless noted) | Notes |
|---|---|---|
| `Units` | `Length`, `Duration` (`readonly struct`) | Store metres/seconds; factory methods per unit; reject non-finite |
| `Geometry` | `Point2` (`readonly struct`), `EuclideanMetric` | Distance in metres; no FMA |
| `Domain` | `PointId`, `DrillPoint`, `DrillJob`, `ConstraintSet`, `PrecedenceEdge`, `LockedGroup`, `Route`, `RouteMetrics` | `PointId` validated, ordinal comparison only |
| `Validation` | `JobValidator`, `ValidationIssue`, `ValidationResult` | Structural + constraint graph checks; deterministic issue order |
| `Optimisation` | `NearestNeighbourConstructor`, `ConstrainedTwoOptImprover`, `SearchBudget`, `SearchStatistics`, `RouteOptimiser` | Pure functions over a `DrillJob` |
| `Verification` | `FeasibilityValidator`, `FeasibilityReport`, `RouteDistanceCalculator` | Must not reference `Optimisation` |
| `Analysis` | `AnalysisEngine`, `AnalysisResult`, `RouteSelection` | Orchestrates validate → optimise → verify → select |

## Rules
- Collections exposed as read-only; constructors validate invariants and throw only for programmer errors.
- Expected failures (invalid input, infeasible constraints) are returned as result objects with stable codes, not exceptions.
- Ordering and tie-breaks use `StringComparer.Ordinal` on `PointId`.
- `CancellationToken` checked once per pass and every 4,096 move evaluations (proposed).
- No static mutable state; no `Random`; no LINQ in hot loops where it obscures order.

## Acceptance criteria
- Every public type has XML docs and unit tests named `<Type>Tests`.
- Domain constructors reject NaN, Infinity, duplicate IDs and unknown references.
- `AnalysisEngine` output is byte-stable when serialised by Application for the same input.

## Risks
- Struct misuse (defensive copies) → keep structs small, `readonly`.
- Over-modelling before need → add types only when a use case requires them.

## Related
[geometry-and-optimization.md](geometry-and-optimization.md) · [ADR-0001](adr/ADR-0001-pure-deterministic-core.md) · [ADR-0006](adr/ADR-0006-si-internal-units.md) · [ARCHITECTURE.md](../../.github/ARCHITECTURE.md)
