# Route visualisation

Status: Proposed | Last reviewed: 2026-11-19

## Purpose and scope
- 2D comparison view of baseline and candidate routes in the web UI.
- Out of scope: 3D, tool/clearance rendering, any depiction implying machine safety.

## Key points
- Cartesian plot, equal aspect ratio, labelled axes with units (mm) and origin.
- Numbered point markers; baseline and candidate polylines; fixed start/end; changed edges highlighted with pattern as well as colour.
- Layer toggles, pan/zoom, hover/focus to inspect ID and coordinates; keyboard operable.
- Equivalent data table for every chart; downsample rendering for large sets without changing computed data.
- SVG/PNG export only after fidelity tests (coordinates match report).

## Acceptance criteria
- Rendered points/edges match report data in automated comparison tests.
- Usable with keyboard only and without colour perception.

## Risks
- Visual implies clearance or safety → no tool radius, no "safe" labels.
- Downsampling hides a point → table always complete.

## Related
[DESIGN.md](../DESIGN.md) · [FRONTEND.md](../FRONTEND.md) · [input-output-contracts.md](input-output-contracts.md)
