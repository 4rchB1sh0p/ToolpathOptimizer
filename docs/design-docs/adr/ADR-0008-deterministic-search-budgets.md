# ADR-0008 — Deterministic search budgets; wall-clock deadline fails closed

Status: Proposed | Date: 2026-11-19 | Log: D-008

## Context
Stopping local search after N seconds makes the result depend on CPU speed and load, breaking reproducibility.

## Decision
- Normal termination: convergence (no improving feasible move) or deterministic budgets: `max_passes` and `max_move_evaluations`.
- Effective budgets are recorded in the report `parameters` and part of the reproducibility key.
- A wall-clock deadline (Application, via `TimeProvider`) is only a safety guard. Reaching it cancels the run: no report, exit code 8 (`OPT_DEADLINE_EXCEEDED`).
- Budget exhaustion is a warning (`SEARCH_BUDGET_EXHAUSTED`), not an error; the result is still deterministic.

## Consequences
- Identical (input hash, algorithm version, parameters) → identical routes on supported platforms.
- Slow machines may hit the deadline where fast ones do not; users lower budgets and rerun.

## Alternatives rejected
- Return best-so-far at deadline: non-reproducible output presented as a result.

## Acceptance criteria
- Test with `FakeTimeProvider` proves deadline → exit 8 and no output file.
- Same job run twice with different machine load yields byte-identical reports (excluding `generated_at_utc`).

## Risks
- Default budgets too low for value or too high for latency → set from plan 0002 benchmarks (TD-009).

## Related
[ADR-0007](ADR-0007-nn-2opt-independent-validator.md) · [cli-design.md](../cli-design.md) · [RELIABILITY.md](../../RELIABILITY.md)
