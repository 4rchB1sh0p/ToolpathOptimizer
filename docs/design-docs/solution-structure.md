# Solution structure

Status: Implemented (scaffold, D-019) | Last reviewed: 2026-11-19

## Purpose and scope
- Defines future projects, folders, dependencies and namespaces.
- Out of scope: class-level design (see [core-library.md](core-library.md), [application-and-infrastructure.md](application-and-infrastructure.md)).

## Layout
```text
src/
  ToolpathOptimizer.Core/            domain, units, geometry, optimisation, verification
  ToolpathOptimizer.Application/     use cases, contracts v1, ports, limits, provenance
  ToolpathOptimizer.Infrastructure/  file system, storage, persistence, identity/billing adapters
  ToolpathOptimizer.Cli/             Generic Host console app, commands, exit codes
  ToolpathOptimizer.Web/             ASP.NET Core API + UI host
tests/
  ToolpathOptimizer.Core.UnitTests/
  ToolpathOptimizer.Application.UnitTests/
  ToolpathOptimizer.Infrastructure.IntegrationTests/
  ToolpathOptimizer.Cli.IntegrationTests/
  ToolpathOptimizer.Web.IntegrationTests/
benchmarks/
  ToolpathOptimizer.Core.Benchmarks/
deploy/                              future container/Compose assets (not in docs phase)
docs/
```

## Dependency rule
| Project | May reference |
|---|---|
| Core | BCL only |
| Application | Core, `Microsoft.Extensions.*.Abstractions` (logging, options, DI) |
| Infrastructure | Application, Core, provider SDKs |
| Cli | Application, Infrastructure (composition root only), hosting packages |
| Web | Application, Infrastructure (composition root only), ASP.NET Core |
| Tests | Their system under test plus test libraries |

## Conventions
- Root namespace = assembly name = folder name.
- Test file/class: `<ClassUnderTest>Tests` (e.g. `NearestNeighbourConstructorTests`).
- Central package management and NuGet lock files; package source mapping.
- Nullable reference types and warnings-as-errors on; analyzers enabled.
- No C# `record`; immutable sealed classes (D-014).
- XML `<summary>` on all types and public members ([clean-coding-practices.md](../../.github/clean-coding-practices.md)).

## Acceptance criteria
- Architecture tests enforce the dependency table and `Core.Verification` ↛ `Core.Optimisation`.
- Solution builds with zero warnings; all test projects follow naming rules.

## Risks
- Infrastructure leaking into Application through convenience references → architecture tests.
- Package version drift → central package management and lock files.

## Related
[ADR-0005](adr/ADR-0005-layered-solution-structure.md) · [ARCHITECTURE.md](../../.github/ARCHITECTURE.md) · [testing-strategy.md](testing-strategy.md) · [Decisions.md](../Decisions.md)
