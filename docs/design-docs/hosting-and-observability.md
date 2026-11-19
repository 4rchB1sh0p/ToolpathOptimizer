# Hosting, configuration and observability

Status: Proposed | Last reviewed: 2026-11-19

## Purpose and scope
- How CLI and Web use Generic Host, DI, options, cancellation, `TimeProvider` and structured logging.
- Out of scope: Core (has none of these; [ADR-0001](adr/ADR-0001-pure-deterministic-core.md)).

## Key points
- **Composition**: `AddToolpathApplication()` and `AddToolpathInfrastructure()` extension methods; hosts add only host-specific services.
- **Options**: bound from configuration sections; validated with `IValidateOptions<T>` and `ValidateOnStart()`. Precedence: defaults < `appsettings` < environment < command line.
- **Time**: inject `TimeProvider`; deadlines via `CancellationTokenSource` created with the provider; tests use `FakeTimeProvider`.
- **Cancellation**: CLI—console lifetime maps Ctrl+C/SIGTERM to cancellation (exit 6); Web—`HttpContext.RequestAborted` linked with the deadline token.
- **Logging**: `LoggerMessage` source generators, stable event IDs per area (1000s Cli, 2000s Application, 3000s Infrastructure, 4000s Web). CLI writes logs to stderr; JSON console formatter optional.
- **Data minimisation**: logs carry job/run IDs, hashes, counts, durations, outcome codes; never coordinates, point IDs, file paths with user names, tokens or metadata.
- **Health (Web)**: `/health/live` (process) and `/health/ready` (dependencies); no sensitive detail in responses.
- **Metrics/tracing (later)**: OpenTelemetry-compatible counters (runs, outcomes, durations); verify package versions at scaffold.

## Acceptance criteria
- Invalid options stop start-up (CLI exit 2; Web fails readiness).
- Tests prove cancellation reaches Core and leaves no partial report.
- Log capture tests assert absence of coordinates and point IDs.

## Risks
- Verbose logs leaking customer geometry → data-minimisation tests.
- CLI start-up overhead from hosting → measure in plan 0003.

## Related
[ADR-0009](adr/ADR-0009-generic-host-and-cross-cutting.md) · [cli-design.md](cli-design.md) · [RELIABILITY.md](../RELIABILITY.md) · [SECURITY.md](../SECURITY.md)
