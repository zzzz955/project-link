# Data — Hand-written data models and loaders

## Files
| file | class | role |
|---|---|---|
| `StageData.cs` | `StageData` | Container: IngameStageInfo + IngameStageNodes[] |
| `StageLoader.cs` | `StageLoader` | Static; loads + caches stage rows from Resources CSVs |

## Symbols
| symbol | kind | note |
|---|---|---|
| `StageData.Info` | field | `IngameStageInfo` — stageId, width, height, timeLimit |
| `StageData.Nodes` | field | `IngameStageNodes[]` — colorId/nodeIndex/x/y per node |
| `StageLoader.Load(int)` | method | static; returns StageData or null (logs error if not found) |

## Rules
- Generated model types live in `Generated/` — do not define them here
- Source data: `shared/datas/` CSVs; runtime data: `Resources/Data/` CSVs (gen:data output)
- `StageLoader` is lazy-loaded and cached; safe to call multiple times per session
