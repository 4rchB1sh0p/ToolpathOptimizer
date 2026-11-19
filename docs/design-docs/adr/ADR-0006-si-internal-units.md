# ADR-0006 — SI internal units with explicit boundary units

Status: Accepted (AGENTS.md rule 3) | Date: 2026-11-19 | Log: D-006

## Context
Drilling data is usually in millimetres or inches. Mixed units cause silent, large errors.

## Decision
- Internal representation: metres (length), seconds (time), as `double`, wrapped in `Length`/`Duration` value types.
- v1 import: millimetres only. CSV headers (`x_mm`) and manifest `units: "mm"` must both be present and agree.
- v1 export: millimetres, every length field suffixed `_mm`, plus `length_unit: "mm"`.
- Conversion happens only in Application contract mapping; Core never sees raw mm.
- Inches and other units are rejected (`INPUT_UNSUPPORTED_UNIT`), not converted.

## Consequences
- mm → m → mm is not always bit-exact (0.001 is not a binary fraction). Reported mm values are derived; tolerance is documented as 1e-9 relative.

## Alternatives rejected
- Unit-less doubles: unit errors become invisible.
- Internal mm: contradicts AGENTS.md.

## Acceptance criteria
- Unit tests for every conversion and for mismatched header/manifest units.
- No public Core API accepts a raw `double` length.

## Risks
- Inch users blocked (TD-007).
- Rounding surprises in exported values → display rounding rules in [input-output-contracts.md](../input-output-contracts.md).

## Related
[core-library.md](../core-library.md) · [geometry-and-optimization.md](../geometry-and-optimization.md)
