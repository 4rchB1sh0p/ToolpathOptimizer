# Geometry and optimisation design

Status: Proposed | Last reviewed: 2026-11-19

## Purpose and scope
- Precise MVP problem, constraint semantics, algorithms, numeric rules and validator.
- In scope: one tool, one operation type, 2D independent drill points, non-cutting rapid travel distance.
- Out of scope (explicit exclusions): obstacle/clamp/fixture avoidance, tool changes, spindle/tool dynamics, acceleration-limited motion, 3D/5-axis, feed/cut-time estimates, G-code interpretation, controller-specific rapid behaviour, work-offset handling.

## Problem
- Input: drill points `p1…pn` with unique IDs, a fixed start `S`, optional fixed end `E`, baseline order (from `sequence`), constraints.
- Output: a feasible visit order (open path `S → … → E?`) that reduces Euclidean rapid distance versus the baseline, or the baseline itself.
- Distance: `D = Σ |r[k+1] − r[k]|` over consecutive positions including the `S` leg and the `E` leg if present. Euclidean, metres internally.
- No optimality guarantee; reports say "heuristic improvement".

## Constraint semantics (v1)
| Constraint | Meaning | Rejection |
|---|---|---|
| Fixed start | Route starts at `S` (a position, not a drill point) | Missing → `INPUT_MISSING_START` |
| Fixed end (optional) | Route ends at `E` | Partial end coordinates → input error |
| Precedence `before → after` | `before` visited earlier (not necessarily adjacent) | Unknown ID → `CONSTRAINT_UNKNOWN_POINT`; cycle → `CONSTRAINT_CYCLE` (exit 4) |
| Locked group | All points of the named `operation_group` are visited **contiguously in baseline order** | Unknown group → `CONSTRAINT_UNKNOWN_GROUP` |
| Allowed transitions / group order | Not supported in v1 | Non-empty → `INPUT_UNSUPPORTED_CONSTRAINT` (TD-007) |

- Locked groups are contracted into **blocks** (entry = first, exit = last member). Precedence edges are lifted to blocks; an edge into/out of a block that conflicts with block order or creates a cycle in the contracted graph → `CONSTRAINT_LOCKED_GROUP_CONFLICT` (exit 4).
- Detection: Kahn's algorithm (ordinal tie-break) detects that a cycle exists; strongly connected components (Tarjan) identify the actual cycle members, reported ordinal-sorted so messages are deterministic.

## Pipeline
1. Parse and map units (Application) → `DrillJob` in metres.
2. Validate structure, limits and constraint graph (`JobValidator`).
3. Evaluate baseline with `FeasibilityValidator` (record violations; baseline may be infeasible).
4. Construct candidate with nearest-neighbour.
5. Improve with constrained 2-opt within budgets.
6. Verify candidate independently; recompute distance.
7. Select route and build `AnalysisResult`.

## Nearest-neighbour construction
- Nodes = singleton points + locked blocks. A node is **eligible** when all its contracted predecessors are visited.
- From the current position, pick the eligible node with minimum squared distance to its entry; ties → ordinal smallest entry `PointId`.
- After a block, the current position is its exit point.
- On an acyclic contracted graph this never dead-ends; a dead end is a defect (exit 5).

## Constrained 2-opt
- Positions `r0 = S`, `r1…rn` points, `r(n+1) = E` if present.
- Move `(i, j)`, `0 ≤ i < j−1`, `j ≤ n`: reverse `r[i+1..j]`.
- Delta: `d(ri, rj) + d(ri+1, rj+1) − d(ri, ri+1) − d(rj, rj+1)`; if `j = n` and no `E`, drop the `rj+1` terms.
- Accept only if delta `< −ε_move` (ε_move = 1e-9 m) **and** feasible:
  - no precedence edge has both endpoints inside the reversed segment (reversal would flip them; edges crossing the boundary keep their order);
  - the segment contains no member of a locked block of size ≥ 2 (would break contiguity or order).
- Delta is computed first (O(1)); feasibility only for improving moves.
- Scan order: `i` ascending, `j` ascending; first improvement accepted; scan continues from the same `i`. A pass ends when the scan completes; stop at a pass with no accepted move (converged) or when a budget is exhausted.
- Budgets (deterministic): `max_passes`, `max_move_evaluations` ([ADR-0008](adr/ADR-0008-deterministic-search-budgets.md)).

## Independent feasibility validator
- Lives in `Core.Verification`; does not reference `Core.Optimisation` or share incremental state.
- Checks: route is a permutation of all point IDs (no missing/duplicate); every precedence edge satisfied; every locked group contiguous and in baseline order; start/end respected; all coordinates finite.
- Recomputes distance by straightforward in-order summation; this value is authoritative in the report.
- The optimiser's running total must match within `1e-9 × D + 1e-12 m`; mismatch or any violation → `OPT_CANDIDATE_REJECTED` (exit 5), no success report.

## Selection
- Baseline feasible and `candidate ≥ baseline − ε_select` → select baseline (`NO_IMPROVEMENT_BASELINE_SELECTED`). Default ε_select = 0.001 mm (`min_improvement_mm`).
- Baseline infeasible → select the validated candidate; warn `BASELINE_VIOLATES_CONSTRAINTS`; delta still reported.
- Baseline distance 0 → `delta_percent` null with `ZERO_BASELINE_DISTANCE`.

## Numeric rules
- `double`, finite-checked at boundaries; no FMA, SIMD or parallel reductions in calculation paths.
- Point identity by `PointId`, never by coordinate equality.
- Distinct points closer than 0.001 mm → `INPUT_COINCIDENT_POINTS` (likely duplicate holes).
- Proposed limits (unbenchmarked, TD-009): ≤ 5,000 points, |x|,|y| ≤ 10,000 mm, ≤ 20,000 precedence edges.
- Complexity: NN O(n²); 2-opt O(n²) evaluations per pass plus O(n) per accepted move.

## Acceptance criteria
- Brute-force comparison for n ≤ 8; zero validator rejections across property tests.
- Candidate never selected when worse than a feasible baseline.
- Bit-identical routes across supported platforms for golden fixtures.
- Every rejection rule above has a dedicated test and error code.

## Risks
- Locked blocks are never relocated by 2-opt (TD-006) → measure on pilot data; consider Or-opt later.
- Large precedence graphs slow feasibility checks → limits and benchmarks.
- Users read "shorter distance" as "faster cycle" → report wording per [ADR-0003](adr/ADR-0003-advisory-only-boundary.md).

## Related
[ADR-0007](adr/ADR-0007-nn-2opt-independent-validator.md) · [core-library.md](core-library.md) · [input-output-contracts.md](input-output-contracts.md) · [testing-strategy.md](testing-strategy.md) · [ARCHITECTURE.md](../../.github/ARCHITECTURE.md)
