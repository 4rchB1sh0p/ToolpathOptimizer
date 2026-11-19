# Glossary

Status: Living | Last reviewed: 2026-11-19

## Purpose and scope
Shared terms used across docs. Definitions are product-specific.

## Terms
| Term | Meaning |
|---|---|
| Advisory | Output for human review; never executed or deployed automatically |
| Baseline | The user's supplied order (`sequence` column) |
| Candidate | Route produced by nearest-neighbour + constrained 2-opt |
| Selected route | Candidate, or baseline when the candidate is not shorter by `min_improvement_mm` |
| Rapid travel distance | Euclidean non-cutting distance between consecutive positions incl. start/end legs |
| Precedence | Point A must be visited before point B |
| Locked group | Points of one `operation_group` visited contiguously in baseline order |
| Feasible | Satisfies all constraints per the independent validator |
| Budget | Deterministic search limit (passes, move evaluations) |
| Deadline | Wall-clock guard; reaching it aborts the run (exit 8) |
| Canonical job | Normalised input used for `input_sha256` |
| Tenant | Team/organisation boundary for data isolation |
| Zone / conduit | IEC 62443-inspired grouping of components and their communication paths |

## Acceptance criteria
- New terms added here when first used in a spec or ADR.

## Risks
- Conflicting definitions in other docs → this file wins; fix the other doc.

## Related
[geometry-and-optimization.md](design-docs/geometry-and-optimization.md) · [input-output-contracts.md](design-docs/input-output-contracts.md)
