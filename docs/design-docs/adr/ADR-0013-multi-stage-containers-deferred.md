# ADR-0013 — Multi-stage container images and Compose (deferred)

Status: Proposed | Date: 2026-11-19 | Log: D-013

## Context
Web and CLI need reproducible, minimal, non-root deployment artifacts. This repository is documentation-only.

## Decision
- Future images use multi-stage builds: SDK stage (restore with lock file, build, test, publish) → pinned runtime stage.
- Runtime: non-root user, read-only root filesystem where feasible, no SDK or shell tools if a chiselled/distroless base is chosen.
- Base images pinned by digest; SBOM and vulnerability scan per build.
- Compose for development and single-host pilots only.
- **No Dockerfile, Compose YAML or `.dockerignore` is generated in the documentation phase.**
- Design: [deployment-and-containers.md](../deployment-and-containers.md).

## Consequences
- Health checks must not depend on a shell/curl inside minimal images.

## Alternatives rejected
- Nixpacks/buildpacks by default: less control over hardening; reconsider if needed.

## Acceptance criteria
- Plan 0006 delivers images that pass scan policy, run as non-root and reproduce CLI golden outputs.

## Risks
- Base image CVEs → scheduled rebuilds; pinned digests updated by automation.

## Related
[deployment-and-containers.md](../deployment-and-containers.md) · [0006 plan](../../exec-plans/active/0006-containers-and-pilot-hardening.md)
