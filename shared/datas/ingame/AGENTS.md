# shared/datas/ingame - In-game Stage Data

## Tables
| file | rows | key |
|------|------|-----|
| `ingame_stage.csv` | One row per stage | `stageId` (PK) |
| `ingame_node_colors.csv` | One row per node group color | `nodeGroupId` (PK) |
| `ingame_stage_info.csv` | Legacy one row per stage | `stageId` (PK) |
| `ingame_stage_nodes.csv` | Legacy node endpoint rows | `stageId + colorId + nodeIndex` |

## Schema
**ingame_stage**
- `stageId` int32 PK - contiguous stage identifier; valid range must be `1..maxStageId`
- `width` int32 NN - grid columns
- `height` int32 NN - grid rows
- `timeLimit` int32 NN - per-stage countdown in seconds; 0 = no limit
- `difficulty` int32 NN - stage difficulty for generator/client display
- `boardEncoding` string(32) NN - board codec id; current value `b36w2-rm-v1`
- `nodeMap` string NN - row-major base36 fixed-width 2-char node group layer; `00` empty, `01..0K` node groups
- `cellMap` string NN - row-major base36 fixed-width 2-char cell layer; `00` empty, `01` obstacle, `02+` gimmicks
- `stageMeta` string NN - compact JSON object for extensible gimmick/generator metadata
- `generatorSeed` uint32 NN - seed used for generated stages; 0 = manually authored/unknown

**ingame_node_colors**
- `nodeGroupId` int32 PK - node group id used by `nodeMap`
- `hexColor` string(16) NN - canonical color rendered by client and stage tool
- `displayName` string(32) NN - authoring label

**ingame_stage_info**
- Legacy table kept until client loader migration is complete.

**ingame_stage_nodes**
- Legacy table kept until client loader migration is complete.

## Rules
- Stage IDs must remain contiguous. Add only `maxStageId + 1`; delete only `maxStageId`; update only existing stages.
- `nodeMap` and `cellMap` length must equal `width * height * 2`.
- A node cell cannot also contain an obstacle or gimmick.
- Node groups use `1..20`; each group present in a stage must have an even node count.
- Client and tools must render node colors from `ingame_node_colors.csv`, not hardcoded palettes.
- Do not manually edit generated copies; edit these source CSVs and run `npm run gen:data`.

## Legacy
- `ingame_stage_info.csv` and `ingame_stage_nodes.csv` are retained for the current Unity client until the stage loader/path rule refactor lands.
