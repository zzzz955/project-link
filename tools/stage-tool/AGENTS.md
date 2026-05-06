# tools/stage-tool - Stage Tool

## Nav
| path | role |
|------|------|
| `src/client/` | Vite React stage editor UI |
| `src/server/` | Local HTTP API for CSV-backed stage CRUD |
| `src/shared/` | Stage codec, types, and validation shared by client/server |

## Rules
- Edit source CSVs under `shared/datas/ingame/`; never edit generated data directly.
- CRUD is explicit by `stageId`; add only `maxStageId + 1`, delete only `maxStageId`, update only existing stages.
- Keep `nodeMap` and `cellMap` encoded as base36 fixed-width 2-char row-major layers.
- Render node colors from `ingame_node_colors.csv`.
- Production builds must not expose stage mutation.
