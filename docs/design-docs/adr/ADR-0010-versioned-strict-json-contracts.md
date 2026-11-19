# ADR-0010 — Versioned, strict JSON contracts and provenance hashing

Status: Proposed | Date: 2026-11-19 | Log: D-010

## Context
Inputs are untrusted and results must be reproducible and auditable. Lenient parsers silently drop unknown constraints.

## Decision
- `schema_version` is `"major.minor"`. Readers reject other majors and any minor newer than they support.
- Reject unknown properties, duplicate properties, comments, trailing commas, NaN/Infinity, depth > 16.
- `System.Text.Json` source generation; `snake_case`; invariant culture.
- Provenance: `input_sha256` = SHA-256 of the canonical job (UTF-8, fixed field order, points in baseline order, round-trip number format); `source_files[]` = SHA-256 and size of raw bytes.
- Writers emit only the declared version; additive optional fields only within a major.
- Field definitions: [input-output-contracts.md](../input-output-contracts.md).

## Consequences
- Older tools reject newer-minor files (fail closed, explicit message).

## Alternatives rejected
- Ignore unknown fields: could drop constraints silently.
- Raw-bytes hash only: whitespace changes alter the reproducibility key.

## Acceptance criteria
- Tests for each rejection rule and for hash stability under whitespace/line-ending changes.
- Every report includes schema version, hashes, algorithm name/version, effective parameters and the canonical job (D-017).

## Risks
- Strictness frustrates users → error messages name field, row and fix.
- Library behaviour differs by version (e.g. duplicate-property handling) → verify at scaffold with tests.

## Related
[input-output-contracts.md](../input-output-contracts.md) · [threat-model.md](../threat-model.md)
