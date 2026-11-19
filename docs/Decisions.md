# Decisions

Status: Living document | Last reviewed: 2026-11-19

## 1. Purpose and scope
- Single baseline of accepted/proposed decisions and known debt, required by [AGENTS.md](../.github/AGENTS.md).
- Sections 2–6 state the current baseline; section 7 is the **canonical debt register**; section 8 is the append-only decision log.
- Detailed rationale lives in ADRs under [design-docs/adr/](design-docs/adr/); log entries stay short and link to them.
- Out of scope: task tracking (see [PLANS.md](PLANS.md)).

## 2. Architecture baseline
| Area | Baseline | Source |
|---|---|---|
| Product boundary | Advisory 2D independent drilling-order analysis; no NC output, no machine control | D-003, [ADR-0003](design-docs/adr/ADR-0003-advisory-only-boundary.md) |
| Layering | Web/CLI → Application → Core; Infrastructure implements Application ports | D-005, [ADR-0005](design-docs/adr/ADR-0005-layered-solution-structure.md) |
| Core purity | No I/O, logging, time, network, DB, AI or web dependencies | D-001, [ADR-0001](design-docs/adr/ADR-0001-pure-deterministic-core.md) |
| Algorithm | Nearest-neighbour + constrained 2-opt + independent feasibility validator | D-007, [ADR-0007](design-docs/adr/ADR-0007-nn-2opt-independent-validator.md) |
| Search termination | Deterministic budgets; wall-clock deadline aborts (fail closed) | D-008, [ADR-0008](design-docs/adr/ADR-0008-deterministic-search-budgets.md) |
| Identity/billing | Behind Application ports; ShipAnySaaS gated by verification | D-012, [ADR-0012](design-docs/adr/ADR-0012-shipanysaas-behind-ports.md) |

## 3. Technology baseline (proposed; verify versions at scaffold)
| Area | Baseline | Source |
|---|---|---|
| Runtime | .NET 10 LTS, C# latest stable for that SDK | D-009 |
| Hosting | .NET Generic Host, DI, options validation on start, `TimeProvider`, `ILogger` | D-009, [ADR-0009](design-docs/adr/ADR-0009-generic-host-and-cross-cutting.md) |
| Types | Sealed immutable classes; small value types as `readonly struct`; **no C# `record`** | D-014 |
| JSON | `System.Text.Json` with source generation, snake_case, strict reading | D-010, [ADR-0010](design-docs/adr/ADR-0010-versioned-strict-json-contracts.md) |
| Containers | Multi-stage images, non-root; deferred until web slice | D-013, [ADR-0013](design-docs/adr/ADR-0013-multi-stage-containers-deferred.md) |
| Database | Not selected | Debt TD-005 |

## 4. Contracts and configuration baseline
| Area | Baseline | Source |
|---|---|---|
| Input | CSV points + JSON manifest, `schema_version` 1.0, mm only | D-002, D-010 |
| Internal units | Metres and seconds (SI) | D-006, [ADR-0006](design-docs/adr/ADR-0006-si-internal-units.md) |
| Output | JSON report v1, lengths in mm with `_mm` suffix; self-contained via embedded `canonical_job` | D-010, D-017 |
| CLI exit codes | 0,2–8 stable; 1 reserved | D-011, [ADR-0011](design-docs/adr/ADR-0011-stable-cli-exit-codes.md) |
| Configuration | Options classes validated at startup; secrets never in repo | D-009 |

## 5. Security and safety baseline
| Area | Baseline | Source |
|---|---|---|
| Safety claims | Never claim collision safety, machine safety, cycle-time savings or standards conformance | D-003 |
| Security principles | IEC 62443-inspired zones/conduits and foundational requirements; **no certification or compliance claim** | D-015, [ADR-0014](design-docs/adr/ADR-0014-iec62443-inspired-security.md) |
| AI | Off by default; never in correctness-critical paths | D-004, [ADR-0004](design-docs/adr/ADR-0004-deterministic-computation-optional-ai.md) |
| Untrusted input | Strict parsing, quotas, tenant isolation | [threat-model.md](design-docs/threat-model.md) |
| Use control | Four-role permission matrix enforced server-side | D-018, [web-and-tenancy.md](design-docs/web-and-tenancy.md) |

## 6. Quality and testing baseline
| Area | Baseline | Source |
|---|---|---|
| Test projects | `<Project>.UnitTests` / `<Scope>.IntegrationTests`; class `<Class>Tests` | D-005, [testing-strategy.md](design-docs/testing-strategy.md) |
| Oracles | Brute force (n ≤ 8), metamorphic, property, golden fixtures | D-016 |
| Determinism | Byte-identical reports (excluding timestamp/tool version) across supported platforms | D-008 |
| Release gate | [QUALITY_SCORE.md](QUALITY_SCORE.md) | — |

## 7. Known debt (canonical register)
| ID | Debt / uncertainty | Impact | Trigger for action | Status |
|---|---|---|---|---|
| TD-001 | Travel time uses distance proxy only | Cannot promise cycle-time savings | Buyer requests time estimates | Open |
| TD-002 | CSV + manifest only | Limited CAM interoperability | Validated customer import need | Open |
| TD-003 | Small synchronous web jobs only | Potential timeouts | Measured workload exceeds budget | Open |
| TD-004 | ShipAnySaaS auth/billing/tenancy contracts unverified; public docs describe TypeScript source templates, not a hosted API | Web identity/billing architecture risk | Before any web implementation | Open (blocking 0005) |
| TD-005 | Database provider and migration strategy not selected | Persistence design incomplete | Plan 0005 start | Open |
| TD-006 | Locked groups cannot be relocated by 2-opt; no Or-opt | Lower improvement on grouped jobs | Pilot data shows benefit | Open |
| TD-007 | Inch input, group-order (`AllowedTransitions`) and multi-tool constraints unsupported | Rejected inputs for some users | Validated demand | Open |
| TD-008 | `.vscode/` contains configuration for an unrelated solution | Confusion; possible leakage of unrelated project names | Before first commit/publication | Open |
| TD-009 | Proposed limits (points, file size, coordinates) not benchmarked | Limits may be too tight/loose | Plan 0002 benchmarks | Open |
| TD-010 | [ARCHITECTURE.md](../.github/ARCHITECTURE.md) lists a single `ToolpathOptimizer.Tests`; superseded by AGENTS.md naming (D-005) | Doc inconsistency | Next ARCHITECTURE.md review | Open |

## 8. Decision log

### Template
```text
### D-NNN — <title>
Date: YYYY-MM-DD | Status: Proposed | Accepted | Superseded by D-NNN
- Decision:
- Why:
- Rejected alternatives:
- Accepted tradeoff:
- Links:
```

### D-001 — Pure deterministic Core
Date: 2026-11-19 | Status: Accepted (AGENTS.md rule 4–5)
- Decision: Core has no I/O, logging, clock, network, DB, AI or web dependencies.
- Why: reuse by CLI and Web; reproducibility; testability.
- Rejected alternatives: logging inside algorithms; Core reading files directly.
- Accepted tradeoff: diagnostics returned as data, adding result types.
- Links: [ADR-0001](design-docs/adr/ADR-0001-pure-deterministic-core.md)

### D-002 — Neutral CSV + JSON manifest input only
Date: 2026-11-19 | Status: Proposed
- Decision: v1 accepts only the documented CSV and manifest; no G-code, DXF or vendor CAM formats.
- Why: avoids interpretation and safety risk; avoids proprietary formats.
- Rejected alternatives: G-code parsing; DXF import.
- Accepted tradeoff: manual export step for users (TD-002).
- Links: [ADR-0002](design-docs/adr/ADR-0002-neutral-input-formats.md)

### D-003 — Advisory-only boundary
Date: 2026-11-19 | Status: Accepted (AGENTS.md rules 1–2)
- Decision: outputs are reports; never machine-ready NC; never safety or time-saving claims.
- Why: no kinematic, fixture or collision model exists.
- Rejected alternatives: post-processed NC export.
- Accepted tradeoff: users re-enter sequence in CAM.
- Links: [ADR-0003](design-docs/adr/ADR-0003-advisory-only-boundary.md)

### D-004 — Deterministic computation; optional AI narrative
Date: 2026-11-19 | Status: Accepted (AGENTS.md rule 7)
- Decision: metrics/constraints are deterministic; any AI summary is opt-in, cites computed values and is labelled non-authoritative.
- Why: correctness and auditability.
- Rejected alternatives: AI-proposed sequences.
- Accepted tradeoff: no AI differentiation in MVP.
- Links: [ADR-0004](design-docs/adr/ADR-0004-deterministic-computation-optional-ai.md)

### D-005 — Layered projects and test naming
Date: 2026-11-19 | Status: Accepted
- Decision: projects per [solution-structure.md](design-docs/solution-structure.md); test projects use AGENTS.md naming, refining ARCHITECTURE.md's single `ToolpathOptimizer.Tests`.
- Why: AGENTS.md testing rules are explicit; one test project per SUT keeps references minimal.
- Rejected alternatives: single shared test project.
- Accepted tradeoff: more projects; TD-010 until ARCHITECTURE.md is updated.
- Links: [ADR-0005](design-docs/adr/ADR-0005-layered-solution-structure.md)

### D-006 — SI internal units
Date: 2026-11-19 | Status: Accepted (AGENTS.md rule 3)
- Decision: internal metres/seconds; v1 import/export in millimetres with explicit unit fields; mismatches rejected.
- Why: avoids unit ambiguity.
- Rejected alternatives: unit-less doubles; internal mm.
- Accepted tradeoff: conversion at boundaries; mm values derived (tolerance documented).
- Links: [ADR-0006](design-docs/adr/ADR-0006-si-internal-units.md)

### D-007 — NN + constrained 2-opt + independent validator
Date: 2026-11-19 | Status: Proposed
- Decision: deterministic nearest-neighbour construction, constrained first-improvement 2-opt, separate validator recomputing feasibility and distance.
- Why: simple, explainable, bounded; validator catches optimiser defects.
- Rejected alternatives: MILP/CP solvers, metaheuristics, Or-tools in MVP.
- Accepted tradeoff: no optimality guarantee; TD-006.
- Links: [ADR-0007](design-docs/adr/ADR-0007-nn-2opt-independent-validator.md)

### D-008 — Deterministic search budgets
Date: 2026-11-19 | Status: Proposed
- Decision: search stops on pass/evaluation budgets; wall-clock deadline aborts without a result (exit 8).
- Why: time-based stopping makes output machine-speed dependent.
- Rejected alternatives: return best-so-far at deadline.
- Accepted tradeoff: users may need to lower budgets and rerun.
- Links: [ADR-0008](design-docs/adr/ADR-0008-deterministic-search-budgets.md)

### D-009 — Generic Host and cross-cutting services
Date: 2026-11-19 | Status: Proposed
- Decision: CLI and Web use Generic Host, DI, validated options, `TimeProvider`, `ILogger` with source-generated messages; target .NET 10 LTS.
- Why: standard, testable, supported.
- Rejected alternatives: hand-rolled composition; static loggers.
- Accepted tradeoff: host start-up cost in CLI (to be measured).
- Links: [ADR-0009](design-docs/adr/ADR-0009-generic-host-and-cross-cutting.md)

### D-010 — Versioned strict JSON contracts and provenance
Date: 2026-11-19 | Status: Proposed
- Decision: `major.minor` schema versions; reject unknown properties, unsupported versions, duplicates and non-finite numbers; canonical input hash plus raw file hashes.
- Why: fail closed; reproducibility.
- Rejected alternatives: lenient reading; raw-bytes hash only.
- Accepted tradeoff: newer-minor files rejected by older readers.
- Links: [ADR-0010](design-docs/adr/ADR-0010-versioned-strict-json-contracts.md)

### D-011 — Stable CLI exit codes
Date: 2026-11-19 | Status: Proposed
- Decision: 0 success, 2 args, 3 input, 4 infeasible, 5 processing, 6 cancelled, 7 I/O, 8 resource budget; 1 reserved.
- Why: orchestrators branch on codes; adds 8 to separate retryable budget failures.
- Rejected alternatives: 0/1 only; POSIX 130 for cancel.
- Accepted tradeoff: codes are a public contract; changes need a major version.
- Links: [ADR-0011](design-docs/adr/ADR-0011-stable-cli-exit-codes.md)

### D-012 — ShipAnySaaS behind ports, gated
Date: 2026-11-19 | Status: Proposed (blocked on verification)
- Decision: identity, tenancy and entitlements are Application ports; no ShipAnySaaS adapter until plan 0004 passes.
- Why: public docs show TypeScript source templates, not a hosted API; contracts unverified.
- Rejected alternatives: building web directly on assumed ShipAnySaaS APIs.
- Accepted tradeoff: possible extra adapter or different provider.
- Links: [ADR-0012](design-docs/adr/ADR-0012-shipanysaas-behind-ports.md), [shipanysaas-integration.md](design-docs/shipanysaas-integration.md)

### D-013 — Multi-stage containers (deferred)
Date: 2026-11-19 | Status: Proposed
- Decision: document multi-stage, non-root images and Compose topology; generate no assets in docs phase.
- Why: reproducible builds, small attack surface.
- Rejected alternatives: Nixpacks/buildpacks by default.
- Accepted tradeoff: hand-maintained Dockerfiles later.
- Links: [ADR-0013](design-docs/adr/ADR-0013-multi-stage-containers-deferred.md)

### D-014 — Classes instead of `record`
Date: 2026-11-19 | Status: Accepted ([clean-coding-practices.md](../.github/clean-coding-practices.md))
- Decision: domain types are sealed immutable classes; small numeric value types (`Length`, `Point2`) may be `readonly struct`.
- Why: repository coding standard forbids `record`; structs avoid allocation in hot loops.
- Rejected alternatives: `record`/`record struct`.
- Accepted tradeoff: explicit equality where needed.
- Links: [core-library.md](design-docs/core-library.md)

### D-015 — IEC 62443-inspired security, no claims
Date: 2026-11-19 | Status: Proposed
- Decision: use zones/conduits and the seven foundational requirements as design input; no security-level or certification claim.
- Why: industrial buyers expect the vocabulary; claims need assessment.
- Rejected alternatives: ignoring OT security; claiming alignment.
- Accepted tradeoff: extra documentation effort.
- Links: [ADR-0014](design-docs/adr/ADR-0014-iec62443-inspired-security.md)

### D-016 — Test oracles
Date: 2026-11-19 | Status: Proposed
- Decision: brute-force optimum for n ≤ 8, metamorphic and property tests, golden fixtures, cross-platform determinism matrix.
- Why: heuristic output has no single expected answer otherwise.
- Rejected alternatives: snapshot-only testing.
- Accepted tradeoff: slower test suite (brute force bounded).
- Links: [testing-strategy.md](design-docs/testing-strategy.md)

### D-017 — Self-contained reports
Date: 2026-11-19 | Status: Proposed
- Decision: report v1 embeds `canonical_job`; parameters are range-checked (non-negative `min_improvement_mm`, integer budgets ≥ 0); CLI refuses output paths that alias an input.
- Why: MVP requires metrics to be independently reproducible from the report; negative tolerance could select a worse route; inputs must be preserved.
- Rejected alternatives: hashes only (cannot recompute); separate export bundle (two artifacts to keep together).
- Accepted tradeoff: larger reports; report contains geometry and must be handled as confidential.
- Links: [input-output-contracts.md](design-docs/input-output-contracts.md), [cli-design.md](design-docs/cli-design.md)

### D-018 — Role permission model
Date: 2026-11-19 | Status: Proposed
- Decision: Owner/Engineer/Reviewer/Viewer action matrix; same-tenant denial 403, cross-tenant 404.
- Why: FR2 use control needs actionable rules beyond tenant isolation.
- Rejected alternatives: tenant-only authorisation; provider-defined roles used directly.
- Accepted tradeoff: role mapping per identity provider.
- Links: [web-and-tenancy.md](design-docs/web-and-tenancy.md)

## Acceptance criteria
- Every baseline row links to a D-entry, ADR or doc.
- Entries are never deleted; superseded entries are marked.
- Debt items are added here, not in other files.

## Risks
- Drift between ADRs and this log → update both in one change.
- Proposed decisions treated as accepted → reviewers check Status before implementing.

## Related
[ARCHITECTURE.md](../.github/ARCHITECTURE.md) · [ADR index](design-docs/architecture-decisions.md) · [PLANS.md](PLANS.md) · [tech-debt-tracker.md](exec-plans/tech-debt-tracker.md)
