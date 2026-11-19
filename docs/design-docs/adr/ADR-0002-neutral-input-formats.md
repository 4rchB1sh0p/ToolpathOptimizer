# ADR-0002 — Neutral CSV + JSON manifest inputs only

Status: Proposed | Date: 2026-11-19 | Log: D-002

## Context
Real drilling data lives in CAM systems and NC programs. Parsing G-code or vendor formats requires controller-specific interpretation and risks proprietary-format use.

## Decision
- v1 accepts only the documented points CSV and job manifest JSON.
- No G-code, DXF, STEP or vendor CAM import in MVP.
- Formats are defined in [input-output-contracts.md](../input-output-contracts.md).

## Consequences
- Users export a point list from CAM manually.
- Parser surface stays small and fully testable.

## Alternatives rejected
- G-code parsing: modal state, canned cycles, offsets and dialects create silent misinterpretation risk.
- DXF import: geometry-to-hole inference is ambiguous.

## Acceptance criteria
- Any non-v1 file is rejected with exit code 3 and a specific error code.
- Sample inputs are synthetic only.

## Risks
- Adoption friction → validate in buyer interviews (plan 0001); TD-002.

## Related
[input-output-contracts.md](../input-output-contracts.md) · [sample-input.md](../../references/sample-input.md) · [future-opportunities.md](../../product-specs/future-opportunities.md)
