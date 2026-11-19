# Testing strategy

Status: Proposed | Last reviewed: 2026-11-19

## Purpose and scope
- How correctness, determinism, robustness, security and performance are proven.
- Applies to all future projects; no tests exist yet.

## Projects and naming (AGENTS.md)
| Project | Content |
|---|---|
| `ToolpathOptimizer.Core.UnitTests` | Units, geometry, validation, NN, 2-opt, validator, property and metamorphic tests |
| `ToolpathOptimizer.Application.UnitTests` | Parsers, mapping, limits, provenance, handlers with fake ports and `FakeTimeProvider` |
| `ToolpathOptimizer.Infrastructure.IntegrationTests` | Atomic writer, storage adapters, persistence (later) |
| `ToolpathOptimizer.Cli.IntegrationTests` | Child-process runs: exit codes, stdout/stderr, cancellation, golden reports |
| `ToolpathOptimizer.Web.IntegrationTests` | `WebApplicationFactory`: tenancy, limits, problem details |
| `ToolpathOptimizer.Core.Benchmarks` | Runtime/memory on declared hardware (not a test project) |

File and class: `<ClassUnderTest>Tests`. One behaviour per test; no network, clock or filesystem in unit tests.

## Oracles
- **Exact geometry**: squares, lines, 3-4-5 triangles with known distances.
- **Brute force**: all permutations for n ≤ 8 respecting constraints; record optimality gap (do not assert optimality).
- **Properties**: candidate is a permutation; all constraints hold; metrics finite and non-negative; selected route never worse than a feasible baseline; validator agrees with optimiser.
- **Metamorphic**: scaling by 2 doubles distance exactly; 90° rotation and axis mirror preserve distance; relabelling IDs preserving ordinal order preserves route; translation preserves distance within tolerance (generators avoid near-ties).
- **Validator self-tests**: deliberately corrupted routes (swap, drop, duplicate, break group) must be rejected.
- **Golden fixtures**: synthetic inputs with committed reports; byte-compare excluding `generated_at_utc`/`tool_version`.

## Determinism and platforms
- CI matrix: win-x64, linux-x64, linux-arm64; golden reports must match across all.
- Run each golden twice per job to catch hidden state.

## Robustness and security
- Negative tests per error code; fuzzing of CSV/JSON readers; limit boundary tests (n, n+1).
- Log-capture tests for data minimisation; cross-tenant tests per endpoint.
- Dependency, secret and container scans as CI gates.

## Tooling (verify versions and licences at scaffold)
Test framework (xUnit proposed), a property-testing library (FsCheck or CsCheck), `Microsoft.Extensions.TimeProvider.Testing`, `Microsoft.AspNetCore.Mvc.Testing`, BenchmarkDotNet, Stryker.NET for mutation testing on Core, an architecture-test library.

## Acceptance criteria
- Every behaviour change ships with tests and docs (AGENTS.md rule 9).
- Core mutation score and branch coverage tracked per release; targets set after first baseline, not invented.
- Benchmarks report hardware, dataset and commit; no performance claim without them.

## Risks
- Flaky tests from time/ordering → `FakeTimeProvider`, ordinal ordering, no parallel shared state.
- Brute-force cost → cap n ≤ 8 and run in a separate category if slow.
- Golden files hiding regressions when bulk-updated → require review note per golden change.

## Related
[QUALITY_SCORE.md](../QUALITY_SCORE.md) · [geometry-and-optimization.md](geometry-and-optimization.md) · [solution-structure.md](solution-structure.md) · [threat-model.md](threat-model.md)
