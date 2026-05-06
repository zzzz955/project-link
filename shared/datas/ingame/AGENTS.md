# shared/datas/ingame — In-game Stage Data

## Tables
| file | rows | key |
|------|------|-----|
| `ingame_stage_info.csv` | One row per stage | `stageId` (PK) |
| `ingame_stage_nodes.csv` | Two rows per color per stage | `stageId + colorId + nodeIndex` |

## Schema
**ingame_stage_info**
- `stageId` int32 PK — unique stage identifier
- `width` int32 NN — grid columns
- `height` int32 NN — grid rows

**ingame_stage_nodes**
- `stageId` int32 NN — references ingame_stage_info.stageId
- `colorId` int32 NN — color identifier (1-based, unique per stage)
- `nodeIndex` int32 NN — 1 = first endpoint, 2 = second endpoint
- `x` int32 NN — column (0-based, left = 0)
- `y` int32 NN — row (0-based, top = 0)

## Sample Data
- Stage 1: 4x4, 2 colors — Red (0,0)→(3,3), Blue (3,0)→(0,3)
- Stage 2: 5x5, 3 colors — Red (0,0)→(4,4), Blue (4,0)→(0,4), Green (2,0)→(2,4)

## Planned Additions
- `ingame_stage_info.csv`: `difficulty`, `time_limit` columns (post-MVP)
