# Toolpath Optimizer — Documentation-first repository

## Purpose and scope
- A C#/.NET product for **advisory, offline-first optimisation of independent 2D drilling sequences**. Not a CAM system.
- This repository contains **Markdown documentation only**: no runnable code, Dockerfiles or Compose YAML.
- Boundary: no G-code or machine-ready NC output, no collision-safety claim, no live controller connectivity.

## Intended solution
| Project | Role |
|---|---|
| `ToolpathOptimizer.Core` | Deterministic, pure domain and algorithms (NN + constrained 2-opt + independent validator) |
| `ToolpathOptimizer.Application` | Use cases, versioned contracts, limits, provenance, ports |
| `ToolpathOptimizer.Infrastructure` | File, storage, persistence, identity/billing adapters |
| `ToolpathOptimizer.Cli` | Offline Generic Host runner with stable exit codes |
| `ToolpathOptimizer.Web` | ASP.NET Core API and UI; identity/billing via ports (ShipAnySaaS only after verification) |
| Tests | `*.UnitTests` / `*.IntegrationTests` per [testing-strategy.md](docs/design-docs/testing-strategy.md) |
| `deploy/` | Future multi-stage images and Compose ([deployment-and-containers.md](docs/design-docs/deployment-and-containers.md)) |

## Start here
1. [AGENTS.md](.github/AGENTS.md) and [ARCHITECTURE.md](.github/ARCHITECTURE.md) (authoritative)
2. [Decisions.md](docs/Decisions.md) and [ADR index](docs/design-docs/architecture-decisions.md)
3. [MVP spec](docs/product-specs/mvp.md) and [design index](docs/design-docs/index.md)
4. [PLANS.md](docs/PLANS.md) and [active plan 0001](docs/exec-plans/active/0001-documentation-to-scaffold.md)

## Acceptance criteria
- Every doc has scope, acceptance criteria, risks and related links; all links resolve.

## Risks
- `.vscode/` holds configuration for an unrelated solution (TD-008) → remove before publishing.

## Related
[SECURITY.md](docs/SECURITY.md) · [GLOSSARY.md](docs/GLOSSARY.md) · [QUALITY_SCORE.md](docs/QUALITY_SCORE.md)
