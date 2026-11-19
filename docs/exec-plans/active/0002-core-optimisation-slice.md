# Execution plan 0002 — Core optimisation slice

Status: Not started (depends on 0001) | Last reviewed: 2026-11-19

## Goal
Deliver `ToolpathOptimizer.Core` with units, domain, validation, nearest-neighbour, constrained 2-opt and the independent validator, fully tested.

## Scope
- In: Core, `Core.UnitTests`, `Core.Benchmarks`, solution scaffold, CI build/test.
- Out: parsing, CLI, Web, persistence.

## Assumptions
- .NET 10 LTS; test and property libraries confirmed in 0001.

## Tasks
- [ ] Scaffold solution per [solution-structure.md](../../design-docs/solution-structure.md); central packages, analyzers, architecture tests.
- [ ] `Length`, `Point2`, `PointId`, domain types; invariant tests.
- [ ] `JobValidator` incl. contracted-graph cycle and locked-group conflicts.
- [ ] `FeasibilityValidator` + corrupted-route tests (before the optimiser).
- [ ] `NearestNeighbourConstructor`, `ConstrainedTwoOptImprover`, budgets.
- [ ] Brute-force, property, metamorphic tests; cross-platform CI matrix.
- [ ] Benchmarks on synthetic sets; set default limits (TD-009).

## Acceptance criteria
- All [geometry-and-optimization.md](../../design-docs/geometry-and-optimization.md) criteria met with test evidence.
- Core references BCL only (architecture test).
- Benchmark report lists hardware, dataset, commit.

## Risks
- Cross-platform floating differences → investigate before relaxing to tolerance comparison.
- 2-opt too slow at upper limits → lower limits, not hidden time cut-offs.

## Rollback
Library not yet consumed; revert commits.

## Related
[ADR-0007](../../design-docs/adr/ADR-0007-nn-2opt-independent-validator.md) · [ADR-0008](../../design-docs/adr/ADR-0008-deterministic-search-budgets.md) · [testing-strategy.md](../../design-docs/testing-strategy.md)
