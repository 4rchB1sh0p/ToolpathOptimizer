# ADR-0011 — Stable CLI exit codes

Status: Proposed | Date: 2026-11-19 | Log: D-011

## Context
Batch orchestrators and scripts branch on exit codes. Budget failures are retryable with smaller budgets; input failures are not.

## Decision
| Code | Meaning |
|---|---|
| 0 | Success; report written (candidate or baseline selected) |
| 1 | Reserved; never emitted intentionally (indicates a host crash defect) |
| 2 | Invalid command-line arguments or configuration |
| 3 | Invalid input (schema, units, parse, input limits) |
| 4 | Infeasible constraints (cycle, locked-group conflict) |
| 5 | Processing failure (internal error, candidate rejected by validator) |
| 6 | Cancelled (Ctrl+C, SIGINT, SIGTERM) |
| 7 | I/O failure (read, write, permission, atomic rename) |
| 8 | Resource budget exceeded (wall-clock deadline) |

- Unhandled exceptions are caught at the top level and mapped to 5.
- The first failure determines the code; cancellation wins over later failures.

## Consequences
- Codes are a public contract; changing meaning requires a CLI major version.

## Alternatives rejected
- 0/1 only: no automation value.
- POSIX 130 for cancel: kept 6 as already documented.

## Acceptance criteria
- Integration test per code, run as a child process.

## Risks
- Runtime-level crashes still return non-mapped codes → documented as defect signal.

## Related
[cli-design.md](../cli-design.md) · [offline-cli-analysis.md](../../product-specs/offline-cli-analysis.md)
