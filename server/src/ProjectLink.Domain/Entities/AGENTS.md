# ProjectLink.Domain/Entities

## Files
| file | class | table |
|------|-------|-------|
| `Session.cs` | `Session` | `sessions` |
| `StageProgress.cs` | `StageProgress` | `stage_progress` (PK: user_id+stage_id) |
| `StageBestRecord.cs` | `StageBestRecord` | `stage_best_records` (PK: user_id+stage_id) |
| `ClientMeta.cs` | `ClientMeta` | `client_meta` |
| `UserCurrency.cs` | `UserCurrency` | `user_currency` |
| `CurrencyLog.cs` | `CurrencyLog` | `currency_logs` (append-only) |
| `StaminaState.cs` | `StaminaState` | `stamina_state` |
| `Inventory.cs` | `Inventory` | `inventory` (PK: user_id+item_id) |
| `UserProfile.cs` | `UserProfile` | `user_profiles` |
| `UserRankingCache.cs` | `UserRankingCache` | `user_ranking_cache` |
| `ActionLog.cs` | `ActionLog` | `action_logs` (append-only) |
| `DailyChallengeProgress.cs` | `DailyChallengeProgress` | `daily_challenge_progress` (PK: user_id+challenge_date) |
| `PlayerSettings.cs` | `PlayerSettings` | `player_settings` |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `UserProfile.AvatarId` | property | default=1; shown in ranking entries and lobby |
| `UserProfile.MaxClearedStageId` | property | highest sequentially-cleared stage (0 = none); updated by atomic conditional UPDATE in `StageEndTransactionRepository` |
| `DailyChallengeProgress.StreakDays` | property | set at COMPLETION time (not at row creation); 0 on new rows |
| `DailyChallengeProgress.LastStreakDate` | property | nullable; date of last consecutive completion |
| `PlayerSettings.Language` | property | default="EN"; ISO 639-1 code |

## Cross-refs
- Mapped by: server `Infrastructure.Persistence.AppDbContext.OnModelCreating`
- Used by: all `Infrastructure.Persistence.*Repository` classes

## Rules
- Plain C# POCOs — no validation logic, no domain methods
- `DailyChallengeProgress.StreakDays` is computed by `Application.DailyChallengeService` from yesterday's row before calling the transaction
- Append-only tables (`currency_logs`, `action_logs`): never UPDATE or DELETE rows
