# ProjectLink.Domain/Interfaces

## Files
| file | interface | role |
|------|-----------|------|
| `IStaticDataService.cs` | `IStaticDataService` | Static game data lookups (in-memory, loaded at startup) |
| `ISessionCache.cs` | `ISessionCache` | JWT/session token Redis cache |
| `IStageSessionCache.cs` | `IStageSessionCache` | Active stage session Redis cache |
| `ISessionRepository.cs` | `ISessionRepository` | Session DB CRUD |
| `IUserProfileRepository.cs` | `IUserProfileRepository` | User profile DB CRUD |
| `IProgressRepository.cs` | `IProgressRepository` | Stage progress DB CRUD |
| `ICurrencyRepository.cs` | `ICurrencyRepository` | Soft currency read/grant/deduct |
| `IStaminaRepository.cs` | `IStaminaRepository` | Stamina read/deduct/add with lazy recharge |
| `IInventoryRepository.cs` | `IInventoryRepository` | Inventory grant/deduct |
| `IRankingRepository.cs` | `IRankingRepository` | Stage best records + ranking cache DB CRUD |
| `IDailyChallengeRepository.cs` | `IDailyChallengeRepository` | Daily challenge progress read + play count increment |
| `IPlayerSettingsRepository.cs` | `IPlayerSettingsRepository` | Player settings get/upsert |
| `IStageEndTransaction.cs` | `IStageEndTransaction` | Atomic stage-end DB transaction (progress+best+stamina+ranking+currency+daily) |
| `IStaminaRefillTransaction.cs` | `IStaminaRefillTransaction` | Atomic full stamina refill (stamina+currency+log) with FOR UPDATE |
| `IDailyChallengeCompleteTransaction.cs` | `IDailyChallengeCompleteTransaction` | Atomic challenge complete (mark done+streak+rewards) |
| `IShopPurchaseTransaction.cs` | `IShopPurchaseTransaction` | Atomic shop purchase (currency deduct+inventory grant) |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `IStageEndTransaction.ExecuteAsync` | method | `StageEndDbCommand` → `StageEndDbResult`; first clear grants soft reward, clear refunds start stamina, uses FOR UPDATE NOWAIT on stage rows |
| `StageEndDbResult.SoftRewardGranted` | property | actual soft currency granted by stage end; 0 on already-cleared replay |
| `StageEndDbResult.TotalScore` | property | post-commit ranking cache total score for Redis ZADD |
| `StageEndDbResult.StagesCleared` | property | post-commit stages cleared count for Redis ZADD |
| `IDailyChallengeRepository.IncrementPlayCountAsync` | method | atomic INSERT ON CONFLICT; returns new play_count |
| `IStaminaRefillTransaction.ExecuteAsync` | method | throws `StaminaAlreadyFullException` or `InsufficientFundsException` |
| `IDailyChallengeCompleteTransaction.ExecuteAsync` | method | throws `DailyChallengeAlreadyCompletedException` or `DailyChallengeNotCompletableException` |

## Cross-refs
- Implemented by: server `Infrastructure.Persistence.*` and `Infrastructure.Data.StaticDataService`
- Consumed by: server `Application.*Service`

## Rules
- Transaction interfaces (`IStageEndTransaction` etc.) encapsulate multi-table atomic writes
- Never call multiple single-row repositories sequentially for operations that require atomicity
- FOR UPDATE NOWAIT: stage rows; FOR UPDATE: currency/ranking rows
