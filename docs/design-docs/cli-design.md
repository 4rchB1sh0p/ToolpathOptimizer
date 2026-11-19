# CLI design

Status: Proposed | Last reviewed: 2026-11-19

## Purpose and scope
- Offline command-line runner for one job per invocation; no network access required.
- Out of scope: batch directories, watch mode, NC output.

## Commands
| Command | Purpose |
|---|---|
| `toolpath-optimize analyze --points <csv> --job <json> --output <path> [--overwrite] [--format json]` | Validate, optimise, verify, write report |
| `toolpath-optimize validate --points <csv> --job <json>` | Validation only; no report file |
| `toolpath-optimize version` | Tool, algorithm and schema versions as JSON |
| `toolpath-optimize help` | Usage |

## Behaviour
- Generic Host with DI, validated options, `TimeProvider`, cancellation ([hosting-and-observability.md](hosting-and-observability.md)).
- **stdout**: exactly one JSON result envelope (`schema_version`, `status`, `exit_code`, `report_path`, `errors[]`). **stderr**: logs and progress only.
- Report written atomically (temp file in same directory, then rename); existing output refused without `--overwrite` (`IO_OUTPUT_EXISTS`).
- Output path that resolves to either input file (after full-path normalisation, links and file identity) is rejected even with `--overwrite` (`ARGS_OUTPUT_IS_INPUT`, exit 2).
- Inputs opened read-only; size checked before full read; output directory must exist.
- Ctrl+C/SIGTERM → cancellation, temp file removed, exit 6.
- Error text includes codes and locations, never secrets or full input lines.

## Exit codes
`0` success · `1` reserved · `2` invalid arguments/configuration · `3` invalid input · `4` infeasible constraints · `5` processing failure · `6` cancelled · `7` I/O failure · `8` resource budget exceeded. Defined in [ADR-0011](adr/ADR-0011-stable-cli-exit-codes.md).

## Acceptance criteria
- Child-process integration tests for each exit code, stdout envelope schema and stderr separation.
- Cancellation test leaves no partial or temp file.
- Output-equals-input test (incl. symbolic/hard link alias) proves inputs are never replaced.
- Same inputs → identical report bytes except `generated_at_utc`.
- Runs with network disabled.

## Risks
- Command-line parser library maturity/licence → verify at scaffold; isolate behind a small adapter.
- Windows vs POSIX signal differences → test both.
- Atomic rename semantics differ across file systems (network shares) → document local-disk requirement.

## Related
[offline-cli-analysis.md](../product-specs/offline-cli-analysis.md) · [input-output-contracts.md](input-output-contracts.md) · [RELIABILITY.md](../RELIABILITY.md) · [0003 plan](../exec-plans/active/0003-application-and-cli-slice.md)
