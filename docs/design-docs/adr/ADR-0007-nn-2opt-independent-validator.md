# ADR-0007 — Nearest-neighbour + constrained 2-opt with an independent validator

Status: Proposed | Date: 2026-11-19 | Log: D-007

## Context
The problem is an open-path TSP variant with precedence and locked-group constraints. Exact solvers are heavy and hard to explain; users need a feasible, explainable improvement.

## Decision
- Construction: deterministic nearest-neighbour over *eligible* nodes (predecessors visited; locked groups contracted to blocks).
- Improvement: first-improvement 2-opt in fixed `(i, j)` scan order; compute distance delta first, then check feasibility.
- Verification: `FeasibilityValidator` in `ToolpathOptimizer.Core.Verification` independently checks permutation, precedence, locked groups, endpoints and recomputes distance. It must not reference the `Optimisation` namespace.
- Selection: if baseline is feasible and candidate is not shorter by more than ε, the baseline is selected.
- Validator rejection of a candidate is a defect: exit 5, no success report.
- Details: [geometry-and-optimization.md](../geometry-and-optimization.md).

## Consequences
- No global optimality claim; reports say "heuristic improvement".
- Locked blocks of size ≥ 2 are not moved by 2-opt (TD-006).

## Alternatives rejected
- MILP/CP solvers: dependency size, licensing review, explainability.
- Simulated annealing/genetic: randomness complicates determinism.
- Reusing optimiser incremental state in validation: not independent.

## Acceptance criteria
- Every candidate passes the validator in all tests; validator has its own suite including deliberately corrupted routes.
- Gap to brute-force optimum on small fixtures is measured and reported, not assumed.

## Risks
- Shared helper bug affects both optimiser and validator → validator uses its own distance summation and no shared incremental code.
- O(n²) per pass too slow for large n → budgets ([ADR-0008](ADR-0008-deterministic-search-budgets.md)) and limits.

## Related
[geometry-and-optimization.md](../geometry-and-optimization.md) · [testing-strategy.md](../testing-strategy.md) · [ADR-0008](ADR-0008-deterministic-search-budgets.md)
