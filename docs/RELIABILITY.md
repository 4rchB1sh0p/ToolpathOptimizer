# Reliability requirements

Status: Proposed | Last reviewed: 2026-11-19

## Purpose and scope
- Failure behaviour, durability and recovery for CLI and Web.

## Key points
- **CLI**: no network required; bounded parsing; atomic report writes; cancellation propagation; stable non-zero exit codes ([ADR-0011](design-docs/adr/ADR-0011-stable-cli-exit-codes.md)).
- **Fail closed**: invalid input, infeasible constraints, validator rejection and deadline expiry produce no success report.
- **Determinism**: search ends on deterministic budgets; wall-clock deadline aborts ([ADR-0008](design-docs/adr/ADR-0008-deterministic-search-budgets.md)).
- **Web (later jobs)**: idempotency keys, input hashes, durable status, bounded retries for infrastructure faults only (never for invalid input), dead-letter visibility.
- **Immutability**: source files and results preserved; never silently rerun under a new algorithm version.
- **Error classes**: outages reported separately from invalid engineering input.
- **Data**: backup/restore and retention defined and tested before storing customer data.

## Acceptance criteria
- Tests for cancellation (no partial file), deadline (exit 8), I/O failure (exit 7) and idempotent retries.
- Restore drill documented before pilot.

## Risks
- Retrying deterministic failures wastes capacity → retry only transient categories.
- Network file systems break atomic rename → document local-disk requirement.

## Related
[cli-design.md](design-docs/cli-design.md) · [hosting-and-observability.md](design-docs/hosting-and-observability.md) · [deployment-and-containers.md](design-docs/deployment-and-containers.md)
