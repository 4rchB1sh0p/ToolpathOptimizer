# Industrial engineering standards approach

Status: Proposed | Last reviewed: 2026-11-19

## Purpose and scope
- How external standards inform design without conformance claims.

## Key points
| Standard | Use in this product |
|---|---|
| ISO 6983 (G-code) | Interoperability context only; MVP neither parses nor generates NC |
| ISO 14649 (STEP-NC), ISO 10303 (STEP) | Future research topics; not implemented |
| IEC 62443 (industrial security) | Design inspiration: zones/conduits, FR1–FR7, secure development practices; no certification or compliance claim ([ADR-0014](adr/ADR-0014-iec62443-inspired-security.md)) |

Engineering practices applied: explicit units, traceable inputs, versioned algorithms, independent verification, change review, fail-closed handling of unsupported cases, test evidence.

## Acceptance criteria
- No document, UI or report claims conformance with any listed standard.
- Standard editions and licensing verified before any external reference.

## Risks
- Paraphrasing paid standards → cite clause numbers only after licensed access; never reproduce text.

## Related
[SECURITY.md](../SECURITY.md) · [ADR-0003](adr/ADR-0003-advisory-only-boundary.md) · [core-beliefs.md](core-beliefs.md)
