# Execution plan 0003 — Application and CLI slice

Status: Not started (depends on 0002) | Last reviewed: 2026-11-19

## Goal
End-to-end offline analysis: CSV + manifest in, verified advisory report out, stable exit codes.

## Scope
- In: Application (contracts v1, limits, provenance, handlers), Infrastructure file adapters, Cli, related test projects.
- Out: Web, persistence, identity.

## Assumptions
- Contracts approved in 0001.

## Tasks
- [ ] Strict CSV and JSON readers, mm ↔ m mapping, canonical hashing.
- [ ] `AnalyzeJobHandler`, `ValidateJobHandler`; `OperationOutcome` → exit codes.
- [ ] `AtomicFileReportWriter`.
- [ ] Generic Host CLI: options validation, `TimeProvider`, cancellation, stderr logging, stdout envelope.
- [ ] Child-process tests for exit codes 0, 2–8; cancellation; offline run.
- [ ] Golden synthetic fixtures; JSON Schema files for v1.
- [ ] Measure CLI start-up and end-to-end time.

## Acceptance criteria
- [cli-design.md](../../design-docs/cli-design.md) and [offline-cli-analysis.md](../../product-specs/offline-cli-analysis.md) criteria met.
- Log-capture tests show no coordinates or point IDs.

## Risks
- CLI parser library immaturity → thin adapter.
- Windows/Linux signal behaviour → test both.

## Rollback
Not distributed until signed; revert commits.

## Related
[application-and-infrastructure.md](../../design-docs/application-and-infrastructure.md) · [input-output-contracts.md](../../design-docs/input-output-contracts.md) · [ADR-0011](../../design-docs/adr/ADR-0011-stable-cli-exit-codes.md)
