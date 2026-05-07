# ProjectLink.Domain/StaticData

## Files
| file | class | role |
|------|-------|------|
| `IngameStageData.cs` | `IngameStageData` | POCO for one ingame_stage CSV row |
| `IngameItemData.cs` | `IngameItemData` | POCO for one ingame_item CSV row |
| `OutgameStaminaConfigData.cs` | `OutgameStaminaConfigData` | Global stamina config (single row) |
| `OutgameAvatarData.cs` | `OutgameAvatarData` | Preset avatar definitions |
| `OutgameDailyChallengeData.cs` | `OutgameDailyChallengeData` | Daily challenge global config (single row) |
| `OutgameDailyRewardData.cs` | `OutgameDailyRewardData` | 7-day streak reward schedule rows |
| `OutgameShopCatalogData.cs` | `OutgameShopCatalogData` | Shop product catalog rows |
| `OutgameSeasonEventData.cs` | `OutgameSeasonEventData` | Season event definitions |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `IngameStageData.StageId` | property | lookup key for `IStaticDataService.GetStage(stageId)` |
| `IngameStageData.TimeLimit` | property | `score = TimeLimit * 100 - elapsed_cs`; 0 = no time limit |
| `IngameStageData.MoveLimit` | property | 0 = unlimited; validated in StageService.EndAsync |
| `IngameStageData.SoftReward` | property | soft currency granted on first clear; source: `soft_reward` col |
| `IngameItemData.Type` | property | `POWER_UP` = in-game consumable; `OBSTACLE_REMOVE` / `NODE_PAIR_REMOVE` = board items |
| `IngameItemData.CostSoft` | property | authoritative cost; client-provided cost is not trusted |
| `OutgameStaminaConfigData.RefillCostSoft` | property | S-scope (server only) — full refill cost; never sent to client |
| `OutgameShopCatalogData.IsEnabled` | property | false = hidden from catalog response |
| `OutgameSeasonEventData.RankingMetric` | property | S-scope (server only) — Redis key suffix for event leaderboard |

## Cross-refs
- Depends on: `shared/datas/ingame/` + `shared/datas/outgame/` → `server/generated/data/` (gen pipeline)
- Consumed by: server `Infrastructure.Data.StaticDataService` (CSV → POCO at startup)
- Consumed by: server `Application.StageService`, `StaminaService`, `ShopService`, `DailyChallengeService` (via `IStaticDataService`)

## Rules
- Read-only POCOs — no logic, no dependencies
- Source of truth: `shared/datas/**/*.csv`; loaded from `generated/data/**/*.csv`
- Run `npm run gen:data` to regenerate after CSV changes
- `IngameStageData`: column order in generated CSV = stageId,width,height,timeLimit,moveLimit,difficulty,boardEncoding,nodeMap,cellMap,soft_reward,stageMeta...,generatorSeed
