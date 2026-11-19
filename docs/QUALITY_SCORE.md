# Quality scorecard

Status: Active release gate | Last reviewed: 2026-11-19

## Purpose and scope
- Release gate. Track evidence, not vanity scores.

## Gate
| Dimension | Acceptance evidence |
|---|---|
| Geometry correctness | Exact known-distance cases, unit conversion tests, coincident/duplicate-point rejection |
| Constraint correctness | Infeasible jobs rejected (exit 4); every candidate passes the independent validator |
| Determinism | Byte-identical golden reports on win-x64, linux-x64, linux-arm64 |
| Regression | Golden fixtures, brute-force comparison (n ≤ 8), metamorphic tests |
| Performance | Measured runtime/memory on declared hardware and datasets (no invented numbers) |
| CLI | Exit-code tests, cancellation, machine-readable stdout, offline run |
| Web | Tenant isolation, access control, size/rate limits |
| Security | Threat-model mitigations evidenced; dependency, secret and image scans |
| Claims | No safety, certification, optimality or time-saving claims in UI/docs |
| UX | Engineers can explain candidate changes from the report |

## Acceptance criteria
- A dimension is "met" only with linked test results or reviewed evidence.
- Each plan evidences the rows applicable to its slice; the full scorecard is required before pilot (plan 0006 closure).

## Risks
- Gate skipped under schedule pressure → plans cannot move to `completed/` without it.

## Related
[testing-strategy.md](design-docs/testing-strategy.md) · [threat-model.md](design-docs/threat-model.md) · [PLANS.md](PLANS.md)
