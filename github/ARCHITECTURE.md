# System architecture

## Context and boundary
A manufacturing engineer imports a neutral CSV/JSON list of independent drill locations, selects constraints, runs an advisory optimisation, compares baseline and proposed order, and exports an audit report. The browser is not a CNC controller.

```text
Browser / ShipAnySaaS shell (identity, tenant, billing)
       | HTTPS / authenticated API
ASP.NET Core Web API + application orchestration
       | job request / persisted artifacts
Background job worker (future) -> Core algorithms
CLI (offline) ----------------> Core algorithms
       |                             |
       +---- JSON/CSV reports <-------+
Database / object storage (future; per-tenant)
```

## Logical projects (future)
- `ToolpathOptimizer.Core`: geometry, validation, constraints, scoring, optimisation interfaces, results.
- `ToolpathOptimizer.Application`: use cases, DTO mapping, quotas, provenance, orchestration.
- `ToolpathOptimizer.Cli`: command parser, file I/O, exit codes, stdout JSON, stderr diagnostics.
- `ToolpathOptimizer.Web`: ASP.NET Core endpoints, auth integration, persistence and UI.
- `ToolpathOptimizer.Infrastructure`: storage, job execution, external services.
- `ToolpathOptimizer.Tests`: deterministic regression/property/integration suites.

Dependency rule: Web/CLI -> Application -> Core; Infrastructure implements Application ports; Core depends on no outer project.

## Domain model
`DrillPoint(Id, X, Y, OperationGroup, ToolId, Metadata)`, `Job(Start, End?, Units, Points, Constraints)`, `Constraints(FixedPrecedence, LockedGroups, AllowedTransitions, TravelModel)`, `Route(OrderedIds)`, `Metrics(RapidDistance, EstimatedTravelTime?, Violations)`, `AnalysisResult(Baseline, Candidate, Delta, InputHash, AlgorithmVersion, Warnings)`.

## Algorithm pipeline
Parse -> schema/units validation -> canonicalisation -> constraint validation -> baseline evaluation -> feasible initial solution (nearest neighbour) -> bounded 2-opt/local search -> feasibility recheck -> metrics and comparison -> immutable report.

Optimise **non-cutting travel only** in MVP. Never reorder operations with unspecified dependencies. Time estimates are optional and clearly distinguish idealised from machine-realistic travel; rapid rates are not guaranteed actual motion profiles.

## Design principles
Deterministic, explainable, fail closed on ambiguity, bounded resource usage, explicit interfaces, portable core, reproducible artifacts. See `docs/design-docs/`.
