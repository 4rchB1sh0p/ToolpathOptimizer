# Proposed database schema (not generated from migrations)

Status: Design sketch only; no database or migrations exist | Last reviewed: 2026-11-19

## Purpose and scope
- Logical tables for the web product. Replace with generated schema when migrations exist (plan 0005).

## Tables
- `tenants`: id, external_tenant_ref (from identity provider; identity data not duplicated).
- `projects`: id, tenant_id, name, created_at, owner_id.
- `input_artifacts`: id, project_id, tenant_id, schema_version, object_key, input_sha256, source_files_json, uploaded_at, uploaded_by.
- `analysis_runs`: id, project_id, tenant_id, input_artifact_id, algorithm_name, algorithm_version, parameters_json, idempotency_key, status, outcome_code, created_at, completed_at, initiated_by.
- `analysis_results`: run_id, tenant_id, selected_route, baseline_distance_mm, candidate_distance_mm, report_object_key, warnings_json.
- `review_annotations`: id, tenant_id, run_id, author_id, text, created_at.
- `audit_events`: id, tenant_id, actor_id, action, resource_id, occurred_at.

## Rules
- Every tenant-owned table has `tenant_id`, a composite index starting with it, and tenant-scoped queries only.
- Inputs, runs and results are append-only; deletion is a tenant-level purge job with audit.
- Provider and migration strategy not selected (TD-005); do not assume one.

## Acceptance criteria
- Cross-tenant query tests per repository method.

## Risks
- Shared database with an identity template → keep separate databases.

## Related
[web-and-tenancy.md](../design-docs/web-and-tenancy.md) · [application-and-infrastructure.md](../design-docs/application-and-infrastructure.md) · [Decisions.md](../Decisions.md)
