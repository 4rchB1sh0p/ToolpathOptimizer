# Synthetic sample input

Status: Illustrative (v1 contract) | Last reviewed: 2026-11-19

## Purpose and scope
- Synthetic example of the v1 inputs. Not CNC instructions; must not be used for machine execution.

## Example `holes.csv`
```csv
point_id,x_mm,y_mm,sequence,operation_group
H01,0,0,1,A
H02,100,0,2,A
H03,100,100,3,A
H04,0,100,4,A
```

## Example `job.json`
```json
{"schema_version":"1.0","coordinate_system":"XY_CARTESIAN","units":"mm","start_x_mm":0,"start_y_mm":0,"constraints":{"precedence":[],"locked_groups":[]},"algorithm":{"name":"nearest-neighbor-2opt","seed":0}}
```

## Acceptance criteria
- Examples validate against [input-output-contracts.md](../design-docs/input-output-contracts.md) v1 and become golden fixtures in plan 0003.

## Risks
- Examples drift from the contract → update both in one change.

## Related
[input-output-contracts.md](../design-docs/input-output-contracts.md) · [ADR-0002](../design-docs/adr/ADR-0002-neutral-input-formats.md)
