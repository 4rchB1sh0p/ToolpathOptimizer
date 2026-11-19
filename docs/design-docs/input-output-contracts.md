# Input/output contracts

Status: Proposed (v1) | Last reviewed: 2026-11-19

## Purpose and scope
- Defines v1 points CSV, job manifest JSON, report JSON, error codes and compatibility rules.
- Out of scope: G-code, DXF, vendor formats ([ADR-0002](adr/ADR-0002-neutral-input-formats.md)); NC output ([ADR-0003](adr/ADR-0003-advisory-only-boundary.md)).

## Points CSV v1
- UTF-8 (optional BOM stripped), comma-delimited, LF or CRLF, no quoting, no comments.
- Header must be exactly `point_id,x_mm,y_mm,sequence,operation_group`; `operation_group` values may be empty.
- `point_id`, `operation_group`: `^[A-Za-z0-9][A-Za-z0-9_.-]{0,63}$` (also prevents spreadsheet formula injection).
- `x_mm`, `y_mm`: invariant-culture decimals, optional sign and exponent; no thousands separators, hex or NaN/Infinity.
- `sequence`: integers forming exactly `1…n` (defines the baseline order).
- Reject: duplicate IDs, missing/extra columns, empty rows, coincident points, limits exceeded.

## Job manifest v1
| Field | Rule |
|---|---|
| `schema_version` | `"1.0"` |
| `job_id` | Optional; same pattern as `point_id` |
| `coordinate_system` | `"XY_CARTESIAN"` only |
| `units` | `"mm"` only; must match CSV header suffix |
| `start_x_mm`, `start_y_mm` | Required |
| `end_x_mm`, `end_y_mm` | Optional; both or neither |
| `constraints.precedence` | Array of `{ "before": id, "after": id }` |
| `constraints.locked_groups` | Array of `operation_group` names |
| `algorithm.name` | `"nearest-neighbor-2opt"` |
| `algorithm.seed` | Optional; must be `0` (reserved) |
| `algorithm.max_passes`, `algorithm.max_move_evaluations` | Optional integers, `0 ≤ value ≤ configured cap`; `max_passes = 0` means construction only |
| `algorithm.min_improvement_mm` | Optional finite number, `0 ≤ value ≤ configured cap`; default 0.001; negative or non-finite rejected (`INPUT_INVALID_PARAMETER`) |

Unknown properties, duplicate properties, comments, trailing commas and depth > 16 are rejected ([ADR-0010](adr/ADR-0010-versioned-strict-json-contracts.md)). Example: [sample-input.md](../references/sample-input.md).

## Report JSON v1
| Field | Notes |
|---|---|
| `schema_version`, `report_kind` (`advisory_drilling_sequence`), `advisory_notice` | Fixed advisory text |
| `job_id`, `input_sha256`, `source_files[]` (`role`, `sha256`, `size_bytes`) | Provenance |
| `tool_name`, `tool_version`, `algorithm_name`, `algorithm_version`, `parameters` | Effective budgets included |
| `length_unit` (`mm`) | |
| `canonical_job` | Points (`point_id`, `x_mm`, `y_mm`, `operation_group`), start/end, constraints exactly as hashed; makes the report self-contained so distances and feasibility can be recomputed |
| `baseline_order[]`, `candidate_order[]`, `selected_route` (`candidate`/`baseline`), `selection_reason` | |
| `baseline_distance_mm`, `candidate_distance_mm`, `delta_distance_mm`, `delta_percent` | `delta = candidate − baseline` (negative = shorter); percent nullable |
| `constraint_check.baseline`, `constraint_check.candidate` (`feasible`, `violations[]`) | From the independent validator |
| `search` (`passes`, `move_evaluations`, `accepted_moves`, `termination`) | `converged` or `budget_exhausted` |
| `warnings[]` (`code`, `message`) | |
| `generated_at_utc` | ISO 8601 from `TimeProvider`; excluded from determinism comparisons |

- Numbers use shortest round-trip formatting; UI/CSV display rounds to 0.001 mm.
- Optional CSV summary export escapes any cell beginning with `=`, `+`, `-`, `@` in text columns.

## Error and warning codes (stable)
| Code | Exit |
|---|---|
| `ARGS_*` (e.g. `ARGS_MISSING_OPTION`, `ARGS_OUTPUT_IS_INPUT`), `CONFIG_INVALID` | 2 |
| `INPUT_*` (e.g. `INPUT_DUPLICATE_POINT_ID`, `INPUT_NON_FINITE_COORDINATE`, `INPUT_UNIT_MISMATCH`, `INPUT_UNSUPPORTED_UNIT`, `INPUT_UNSUPPORTED_SCHEMA_VERSION`, `INPUT_UNKNOWN_FIELD`, `INPUT_SEQUENCE_INVALID`, `INPUT_COINCIDENT_POINTS`, `INPUT_LIMIT_EXCEEDED`, `INPUT_UNSUPPORTED_CONSTRAINT`, `INPUT_INVALID_PARAMETER`) | 3 |
| `CONSTRAINT_UNKNOWN_POINT`, `CONSTRAINT_UNKNOWN_GROUP` | 3 |
| `CONSTRAINT_CYCLE`, `CONSTRAINT_LOCKED_GROUP_CONFLICT` | 4 |
| `OPT_CANDIDATE_REJECTED`, `INTERNAL_ERROR` | 5 |
| `CANCELLED` | 6 |
| `IO_READ_FAILED`, `IO_WRITE_FAILED`, `IO_OUTPUT_EXISTS` | 7 |
| `OPT_DEADLINE_EXCEEDED` | 8 |
| Warnings: `BASELINE_VIOLATES_CONSTRAINTS`, `NO_IMPROVEMENT_BASELINE_SELECTED`, `SEARCH_BUDGET_EXHAUSTED`, `ZERO_BASELINE_DISTANCE` | 0 |

Messages name the file, row/field and expected value; they never echo more than 64 characters of input.

## Compatibility
- Additive optional fields only within a major; breaking change → new major and parallel reader.
- Readers reject newer minors (fail closed). JSON Schema files are published with the scaffold.

## Acceptance criteria
- One test per rejection rule and per error code; round-trip tests for report v1.
- Determinism: identical inputs → identical report bytes except `generated_at_utc` and `tool_version`.

## Risks
- Strict CSV rejects common exports (quoted fields, `;` delimiter) → clear messages; revisit after interviews.
- Error messages leaking data → truncation rule above.

## Related
[ADR-0010](adr/ADR-0010-versioned-strict-json-contracts.md) · [geometry-and-optimization.md](geometry-and-optimization.md) · [cli-design.md](cli-design.md) · [threat-model.md](threat-model.md)
