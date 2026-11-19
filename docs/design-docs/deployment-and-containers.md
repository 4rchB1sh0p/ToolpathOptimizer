# Docker and Compose design (documentation only)

Status: Proposed — deferred to plan 0006 | Last reviewed: 2026-11-19

## Purpose and scope
- Describes future container images and Compose topology. **No Dockerfile, Compose YAML, `.dockerignore` or `.env` file is generated in this phase.**
- Future asset locations: `src/ToolpathOptimizer.Web/Dockerfile`, `src/ToolpathOptimizer.Cli/Dockerfile`, `deploy/compose.yaml`, `deploy/compose.dev.yaml`, `.dockerignore`, `.env.example`.

## Image build (multi-stage)
1. **restore**: SDK image pinned by digest; copy only project/lock files; locked restore.
2. **build/test**: build with warnings-as-errors; run unit tests; fail the build on test failure.
3. **publish**: Release publish; optional trimming only after tests prove no reflection breakage.
4. **runtime**: pinned ASP.NET (web) or runtime (CLI) image, preferably a minimal/chiselled variant; non-root user; no SDK, compilers or package managers.
- Generate SBOM and run vulnerability scan per image; sign images before customer distribution.

## Services (Compose, development and single-host pilots)
| Service | Notes |
|---|---|
| `web` | Non-root, read-only root filesystem, writable temp volume only, liveness/readiness probes |
| `worker` (later) | Same binaries, separate identity, bounded concurrency |
| `db` | Provider not selected (TD-005); no host port published by default |
| `migrate` | One-shot migration job before `web` starts |
| `storage` | Local volume in dev; object storage adapter in production |
| `cli` (tools profile) | Read-only input mount, separate writable output mount, `--network none` |

## Compose concerns
Explicit internal/external networks; health checks (no shell/curl dependency in minimal images); named volumes; secrets via files/secret store, never in YAML or images; CPU/memory limits; graceful stop period ≥ request deadline; immutable tags; backup/restore test; TLS termination at a reverse proxy in pilots.

## Acceptance criteria
- Images run as non-root, pass scan policy and reproduce CLI golden reports.
- `db` unreachable from host by default; restore drill documented.
- No proprietary or customer data in images or samples.

## Risks
- Minimal images complicate debugging → separate debug image never deployed to production.
- Compose mistaken for production orchestration → document its limits.

## Related
[ADR-0013](adr/ADR-0013-multi-stage-containers-deferred.md) · [RELIABILITY.md](../RELIABILITY.md) · [threat-model.md](threat-model.md) · [0006 plan](../exec-plans/active/0006-containers-and-pilot-hardening.md) · [nixpacks note](../references/nixpacks-llms.txt.md)
