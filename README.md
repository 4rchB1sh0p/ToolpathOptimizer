# Toolpath Optimizer

## Purpose and scope
- A C#/.NET 10 product for **advisory, offline-first optimisation of independent 2D drilling sequences**. Not a CAM system.
- Boundary: no G-code or machine-ready NC output, no collision-safety claim, no live controller connectivity.
- Markdown under `docs/` remains the design source of truth; code follows it.

## Solution (`ToolpathOptimizer.slnx`)
| Project | Role |
|---|---|
| `src/ToolpathOptimizer.Core` | Pure, deterministic domain and algorithms (NN + constrained 2-opt + independent validator) |
| `src/ToolpathOptimizer.Application` | Use cases, strict v1 contracts, limits, provenance, ports |
| `src/ToolpathOptimizer.Infrastructure` | Atomic file writer, bounded reader, alias detection, dev storage/identity adapters |
| `src/ToolpathOptimizer.Cli` | Offline Generic Host runner with stable exit codes |
| `src/ToolpathOptimizer.Web` | ASP.NET Core API + framework-light UI (Development identity only, D-023) |
| `tests/*.UnitTests`, `tests/*.IntegrationTests` | Per [testing-strategy.md](docs/design-docs/testing-strategy.md) |
| `benchmarks/ToolpathOptimizer.Core.Benchmarks` | BenchmarkDotNet; no performance claim without recorded hardware |

## Build, test, run
Requires the .NET 10 SDK (`global.json`).

```bash
dotnet build ToolpathOptimizer.slnx
```

```bash
dotnet test ToolpathOptimizer.slnx
```

CLI on the synthetic sample (stdout carries one JSON envelope; exit codes per [ADR-0011](docs/design-docs/adr/ADR-0011-stable-cli-exit-codes.md)):

```bash
dotnet run --project src/ToolpathOptimizer.Cli -- analyze --points samples/plate.csv --job samples/plate-job.json --output plate.report.json
```

Web UI and API at http://localhost:5080 (Development environment only):

```bash
dotnet run --project src/ToolpathOptimizer.Web --launch-profile http
```

## Start here
1. [AGENTS.md](.github/AGENTS.md) and [ARCHITECTURE.md](.github/ARCHITECTURE.md) (authoritative)
2. [Decisions.md](docs/Decisions.md) and [ADR index](docs/design-docs/architecture-decisions.md)
3. [MVP spec](docs/product-specs/mvp.md) and [design index](docs/design-docs/index.md)
4. [PLANS.md](docs/PLANS.md)

## Acceptance criteria
- Solution builds with zero warnings; all tests pass.
- Every doc has scope, acceptance criteria, risks and related links; all links resolve.

## Risks
- Web identity, billing and persistence are development stand-ins (TD-004, TD-005, TD-015).
- Cross-platform determinism is not yet proven in CI (TD-014).

## Related
[SECURITY.md](docs/SECURITY.md) · [GLOSSARY.md](docs/GLOSSARY.md) · [QUALITY_SCORE.md](docs/QUALITY_SCORE.md) · [sample-input.md](docs/references/sample-input.md)
