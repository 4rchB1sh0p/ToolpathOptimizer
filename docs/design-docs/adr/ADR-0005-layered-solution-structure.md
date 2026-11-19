# ADR-0005 — Layered solution structure and project naming

Status: Accepted (ARCHITECTURE.md + AGENTS.md) | Date: 2026-11-19 | Log: D-005

## Context
[ARCHITECTURE.md](../../../.github/ARCHITECTURE.md) defines logical projects and a dependency rule. [AGENTS.md](../../../.github/AGENTS.md) defines test-project naming.

## Decision
- Production: `ToolpathOptimizer.Core`, `.Application`, `.Infrastructure`, `.Cli`, `.Web` under `src/`.
- Tests: `ToolpathOptimizer.Core.UnitTests`, `.Application.UnitTests`, `.Infrastructure.IntegrationTests`, `.Cli.IntegrationTests`, `.Web.IntegrationTests` under `tests/`.
- Dependency rule: Web/Cli → Application → Core; Infrastructure → Application (+ Core types); Core → nothing.
- Cli and Web reference Infrastructure only in their composition root.
- Root namespace equals assembly name.

## Consequences
- Refines ARCHITECTURE.md's single `ToolpathOptimizer.Tests` (TD-010).

## Alternatives rejected
- One test project: mixes unit and integration dependencies.
- Separate verification assembly: adds a project before need; namespace rule used instead.

## Acceptance criteria
- Architecture tests enforce the dependency rule and namespace rule.
- No project reference cycles.

## Risks
- Application becoming a dumping ground → keep use cases small; review boundaries per plan.

## Related
[solution-structure.md](../solution-structure.md) · [testing-strategy.md](../testing-strategy.md) · [application-and-infrastructure.md](../application-and-infrastructure.md)
