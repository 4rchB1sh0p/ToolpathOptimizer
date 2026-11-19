# ADR-0001 — Pure, deterministic Core library

Status: Accepted (from AGENTS.md rules 4–5) | Date: 2026-11-19 | Log: D-001

## Context
CLI and Web must produce identical results from the same input. Algorithms need fast, isolated tests.

## Decision
- `ToolpathOptimizer.Core` references only the .NET base class library.
- No filesystem, network, database, logging framework, `TimeProvider`, AI or ASP.NET types.
- `CancellationToken` is allowed; checked at bounded intervals.
- Diagnostics (warnings, violations, search statistics) are returned as data.
- No randomness, culture-sensitive comparison, parallel reductions or fused multiply-add in calculation paths.

## Consequences
- Application layer owns time, logging and provenance.
- Core is portable to any host, including future workers.

## Alternatives rejected
- `ILogger` inside algorithms: couples Core to a framework and adds hidden side effects.
- Parallel search: non-deterministic reduction order.

## Acceptance criteria
- Architecture test fails if Core references any non-BCL assembly.
- Same canonical input produces byte-identical Core output on win-x64, linux-x64, linux-arm64.

## Risks
- Pressure to add "just one" log call → reviewers reject; use result diagnostics.
- Floating-point differences across platforms → cross-platform golden tests.

## Related
[core-library.md](../core-library.md) · [solution-structure.md](../solution-structure.md) · [Decisions.md](../../Decisions.md)
