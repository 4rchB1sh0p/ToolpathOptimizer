# ADR-0009 — Generic Host, DI and cross-cutting services

Status: Proposed | Date: 2026-11-19 | Log: D-009

## Context
CLI and Web need the same configuration, logging, cancellation and time handling. Custom composition code is error-prone.

## Decision
- Target .NET 10 LTS (verify support window at scaffold).
- CLI: `Host.CreateApplicationBuilder`; Web: `WebApplication.CreateBuilder`. Both register Application and Infrastructure through extension methods.
- Options: one options class per concern, `IValidateOptions<T>` + `ValidateOnStart()`; invalid configuration stops start-up.
- Time: `TimeProvider.System` registered as singleton; tests use `FakeTimeProvider`. No `DateTime.Now`/`UtcNow` in Application.
- Logging: `ILogger<T>` with source-generated `LoggerMessage` methods and stable event IDs; CLI logs to stderr only.
- Cancellation: `CancellationToken` flows from host lifetime / request abort to Core.
- Details: [hosting-and-observability.md](../hosting-and-observability.md).

## Consequences
- Core stays free of these services (ADR-0001).

## Alternatives rejected
- Static loggers/clock: untestable.
- Third-party DI containers: unnecessary.

## Acceptance criteria
- Start-up fails fast with exit 2 (CLI) or failed health (Web) on invalid options.
- No direct `DateTime.UtcNow` in Application/Infrastructure (analyzer or architecture test).

## Risks
- Host start-up latency in CLI → measure; acceptable if within documented budget.

## Related
[hosting-and-observability.md](../hosting-and-observability.md) · [cli-design.md](../cli-design.md) · [web-and-tenancy.md](../web-and-tenancy.md)
