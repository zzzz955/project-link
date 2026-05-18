# Data - Hand-written data models and loaders

## Files
| file | class | role |
|---|---|---|
| `StageData.cs` | `StageData` | Container: decoded grid maps + node color dictionary |
| `StageLoader.cs` | `StageLoader` | Static; loads IngameStage + IngameNodeColors, decodes base36 maps |
| `OutgameDataLoader.cs` | `OutgameDataLoader` | Static; loads generated outgame CSVs and ingame item metadata for UI binding |

## Symbols
| symbol | kind | note |
|---|---|---|
| `StageData.Width` | field | int; grid width |
| `StageData.Height` | field | int; grid height |
| `StageData.TimeLimit` | field | int; seconds; 0 = no limit |
| `StageData.NodeMap` | field | `int[,]`; decoded nodeMap; 0=empty, 1-20=groupId |
| `StageData.CellMap` | field | `int[,]`; decoded cellMap; 0=empty, 1=obstacle, 2+=gimmick |
| `StageData.NodeColors` | field | `Dictionary<int,Color>`; nodeGroupId to Unity Color |
| `StageLoader.MaxStageId` | prop | max implemented stage id from `ingame_stage.csv` |
| `StageLoader.Load(int)` | method | static; returns StageData or null (logs error if not found) |
| `OutgameDataLoader.GetEnabledShopProducts(string)` | method | returns enabled shop products sorted by `sortOrder`, optional category filter |
| `OutgameDataLoader.GetAllItems()` | method | returns all ingame items as `IReadOnlyList<IngameItem>` from `ingame_item.csv` |
| `OutgameDataLoader.FindItem(int)` | method | resolves item display metadata from `ingame_item.csv` |
| `OutgameDataLoader.StaminaConfig` | prop | returns global stamina planning config from `outgame_stamina_config.csv` |

## Cross-refs
- Gen output: `client/Assets/Resources/Data/` (runtime CSVs read by loaders via CsvLoader)
- Consumed by: client `Core.InGameController` (calls StageLoader.Load at scene start)
- Consumed by: client `Core.GameContext` (holds selected stageId used by StageLoader)
- Consumed by: client `OutGame.UI.LobbyWireframeController` (catalog max stage for carousel bounds)
- Consumed by: client `InGame.UI.ClearPopup` (catalog max stage for Next button)
- Consumed by: client `Services.StaticCatalogService` (outgame UI catalog/config facade)
- Depends on: `client/Assets/Scripts/Data/Generated/` classes

## Rules
- Generated model types live in `Generated/`; do not define them here.
- Source data: `shared/datas/` CSVs; runtime data: `Resources/Data/` CSVs (gen:data output).
- `StageLoader` is lazy-loaded and cached; safe to call multiple times per session.
- `OutgameDataLoader` must read real generated CSV tables only; do not add hardcoded UI fallback rows.
- Map encoding: base36, 2 chars per cell, row-major; decode index = `(y * width + x) * 2`.
