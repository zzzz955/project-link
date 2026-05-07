# ProjectLink.Infrastructure/Data

## Files
| file | class | role |
|------|-------|------|
| `StaticDataService.cs` | `StaticDataService` | Singleton — loads all CSV files at startup into memory dictionaries |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `StaticDataService.GetStage` | method | O(1) lookup by stageId |
| `StaticDataService.GetItem` | method | O(1) lookup by itemId |
| `StaticDataService.GetAllStages` | method | used by ranking cold-start rebuild and StageService |
| `StaticDataService.GetAllItems` | method | used by StageService to build POWER_UP item counts |
| `StaticDataService.GetStaminaConfig` | method | single-row config; fallback hardcoded if CSV missing |
| `StaticDataService.GetAllAvatars` | method | all preset avatars |
| `StaticDataService.GetDailyChallengeConfig` | method | single-row config; fallback hardcoded if CSV missing |
| `StaticDataService.GetAllDailyRewards` | method | all 7 streak reward rows |
| `StaticDataService.GetDailyReward` | method | O(1) lookup by streakDay (1..7) |
| `StaticDataService.GetShopCatalog` | method | all enabled + disabled products |
| `StaticDataService.GetShopProduct` | method | O(1) lookup by productId |
| `StaticDataService.GetAllSeasonEvents` | method | ordered list of all season events |

## Cross-refs
- Depends on: `server/generated/data/ingame/` and `server/generated/data/outgame/` CSV files
- Depends on: all `Domain.StaticData.*Data` POCOs
- Consumed by: server `Application.StageService`, `StaminaService`, `ShopService`, `DailyChallengeService`, `LobbyService`, `EventController` (via `IStaticDataService`)

## Rules
- Loads from `AppContext.BaseDirectory/generated/data/{ingame,outgame}/`
- Missing CSV → logs warning + returns empty/fallback (does not crash)
- ingame_stage column order after moveLimit addition: stageId[0], width[1], height[2], timeLimit[3], moveLimit[4], difficulty[5], boardEncoding[6], nodeMap[7], cellMap[8], soft_reward[9], stageMeta[10..^1], generatorSeed[^1]
- Run `npm run gen:data` before first server start after adding new CSV files
