# ProjectLink.Domain/StaticData

## Files
| file | class | role |
|------|-------|------|
| `IngameStageData.cs` | `IngameStageData` | POCO for one ingame_stage CSV row loaded at server startup |
| `IngameItemData.cs` | `IngameItemData` | POCO for one ingame_item CSV row loaded at server startup |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `IngameStageData.StageId` | property | lookup key for `IStaticDataService.GetStage(stageId)` |
| `IngameStageData.TimeLimit` | property | used by ranking score formula: `score = TimeLimit * 100 - elapsed_cs` |
| `IngameStageData.SoftReward` | property | soft currency granted on stage clear; 0 = no reward; source: `ingame_stage.csv` col `soft_reward` |
| `IngameItemData.Type` | property | `OBSTACLE_REMOVE` or `NODE_PAIR_REMOVE` — drives validation in item use flow |
| `IngameItemData.CostSoft` | property | authoritative cost; client-provided cost is not trusted |

## Rules
- These are read-only POCOs — no logic, no dependencies
- Source of truth is `shared/datas/ingame/*.csv`; server loads from `generated/data/ingame/*.csv`
- Run `npm run gen:data` to regenerate after CSV changes
