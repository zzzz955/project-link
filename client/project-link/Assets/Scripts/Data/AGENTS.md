# Data — Hand-written data models and loaders

## Files
| file | class | role |
|---|---|---|
| `StageData.cs` | `StageData` | Container: decoded grid maps + node color dictionary |
| `StageLoader.cs` | `StageLoader` | Static; loads IngameStage + IngameNodeColors, decodes base36 maps |

## Symbols
| symbol | kind | note |
|---|---|---|
| `StageData.Width` | field | int; grid width |
| `StageData.Height` | field | int; grid height |
| `StageData.TimeLimit` | field | int; seconds; 0 = no limit |
| `StageData.NodeMap` | field | `int[,]`; decoded nodeMap; 0=empty, 1-20=groupId |
| `StageData.CellMap` | field | `int[,]`; decoded cellMap; 0=empty, 1=obstacle, 2+=gimmick |
| `StageData.NodeColors` | field | `Dictionary<int,Color>`; nodeGroupId → Unity Color |
| `StageLoader.Load(int)` | method | static; returns StageData or null (logs error if not found) |

## Rules
- Generated model types live in `Generated/` — do not define them here
- Source data: `shared/datas/` CSVs; runtime data: `Resources/Data/` CSVs (gen:data output)
- `StageLoader` is lazy-loaded and cached; safe to call multiple times per session
- Map encoding: base36, 2 chars per cell, row-major; decode index = `(y * width + x) * 2`
