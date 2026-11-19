# Application layer and infrastructure adapters

Status: Proposed | Last reviewed: 2026-11-19

## Purpose and scope
- Defines use cases, ports, contract mapping, limits and provenance (Application) and their adapters (Infrastructure).
- Out of scope: HTTP endpoints ([web-and-tenancy.md](web-and-tenancy.md)), CLI parsing ([cli-design.md](cli-design.md)).

## Application
| Area | Design |
|---|---|
| Use cases | `ValidateJobHandler` (query), `AnalyzeJobHandler` (command), later `CreateProjectHandler`, `GetAnalysisHandler` |
| Contracts | `Contracts.V1`: manifest/report DTOs, strict CSV reader and JSON reader/writer over `Stream`; mm ↔ m mapping |
| Limits | `AnalysisLimitsOptions`: max file bytes, points, precedence edges, coordinate magnitude, passes, move evaluations, wall-clock deadline |
| Provenance | `ProvenanceFactory`: canonical hash, source-file hashes, tool/algorithm versions, `generated_at_utc` from `TimeProvider` |
| Errors | `OperationOutcome` with category (Input, Infeasible, Processing, Cancelled, Io, Budget) → mapped to exit codes or HTTP problem types |
| Logging | Counts, hashes, durations only; never coordinates, IDs or metadata |

## Ports (interfaces owned by Application)
| Port | Purpose | MVP adapter |
|---|---|---|
| `IReportWriter` | Persist a report atomically | `AtomicFileReportWriter` (CLI) |
| `IArtifactStore` | Store immutable input/report bytes by tenant-scoped key | `LocalArtifactStore` (dev), object storage later |
| `IAnalysisRunRepository` | Run records and status | Deferred (TD-005) |
| `ICurrentPrincipal`, `ITenantContext` | Authenticated user and tenant | `DevelopmentPrincipal` (Development only); provider adapter after plan 0004 |
| `IEntitlementService` | Plan quotas/credits | `StaticEntitlementService` (dev) |
| `IAuditLog` | Security-relevant events | Structured log sink → DB later |

## Infrastructure rules
- Adapters are thin, map provider errors to `OperationOutcome` categories, and never call Core directly.
- File writes: temp file in target directory → flush → atomic move; refuse overwrite unless requested.
- Every storage key and query includes tenant ID; adapters reject calls without it.

## Acceptance criteria
- Use cases unit-tested with fakes for every port; adapters integration-tested.
- Same handler serves CLI and Web with identical report bytes for the same input and clock.

## Risks
- Port explosion → add ports only when a second implementation or test seam is needed.
- Provider lock-in → adapters isolate SDK types.

## Related
[solution-structure.md](solution-structure.md) · [hosting-and-observability.md](hosting-and-observability.md) · [ADR-0012](adr/ADR-0012-shipanysaas-behind-ports.md) · [db-schema.md](../generated/db-schema.md)
