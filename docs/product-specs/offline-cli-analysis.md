# Offline CLI analysis

Status: Proposed | Last reviewed: 2026-11-19

## Purpose and scope
- An engineer or script runs analysis on a workstation with no network and no account.
- Out of scope: batch folders, scheduling, GUI, NC output.

## User stories
1. As an engineer, I validate my files and see every problem with row/field and fix.
2. As an engineer, I run analysis and get a report file plus a one-line JSON summary on stdout.
3. As an automation owner, I branch on exit codes without parsing text.
4. As a reviewer, I rerun the same files later and get the same route.

## Acceptance criteria
- Commands, envelope and exit codes as in [cli-design.md](../design-docs/cli-design.md).
- Works with networking disabled; no telemetry.
- Existing report never overwritten without `--overwrite`.
- Ctrl+C leaves no partial file and exits 6.

## Risks
- Users expect folder batch mode → orchestrate via scripts; revisit with demand.
- Distribution trust → signed binaries before customer distribution.

## Related
[mvp.md](mvp.md) · [ADR-0011](../design-docs/adr/ADR-0011-stable-cli-exit-codes.md) · [0003 plan](../exec-plans/active/0003-application-and-cli-slice.md)
