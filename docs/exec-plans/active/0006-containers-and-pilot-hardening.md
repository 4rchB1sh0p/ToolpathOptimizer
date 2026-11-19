# Execution plan 0006 — Containers and pilot hardening

Status: Not started (depends on 0005) | Last reviewed: 2026-11-19

## Goal
Reproducible, hardened deployment for a single-customer pilot, with security evidence.

## Scope
- In: Dockerfiles, Compose files, CI image pipeline, scans, SBOM, signing, backup/restore drill, runbooks.
- Out: multi-region, Kubernetes, enterprise on-prem packaging.

## Assumptions
- Pilot customer accepts advisory boundary and synthetic-data onboarding.

## Tasks
- [ ] Implement images and Compose per [deployment-and-containers.md](../../design-docs/deployment-and-containers.md).
- [ ] CI: build, test, scan, SBOM, sign; pinned digests with update automation.
- [ ] Backup/restore drill; retention and deletion verified.
- [ ] Evidence for each FR row in [SECURITY.md](../../SECURITY.md); threat model re-review.
- [ ] Incident and vulnerability-disclosure procedure published.

## Acceptance criteria
- Images non-root, scans within policy, golden CLI reports reproduced in containers.
- [QUALITY_SCORE.md](../../QUALITY_SCORE.md) fully evidenced; plan moved to completed.

## Risks
- Hardening breaks health checks → orchestrator-level probes.
- Pilot data handling → data-processing agreement before upload.

## Rollback
Redeploy previous image digest; restore database from verified backup.

## Related
[ADR-0013](../../design-docs/adr/ADR-0013-multi-stage-containers-deferred.md) · [RELIABILITY.md](../../RELIABILITY.md) · [threat-model.md](../../design-docs/threat-model.md)
