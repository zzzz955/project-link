# ProjectLink.Infrastructure/Persistence

## Files
| file | class | role |
|------|-------|------|
| `AppDbContext.cs` | `AppDbContext` | EF Core DbContext — entity mapping via `OnModelCreating` |
| `SessionRepository.cs` | `SessionRepository` | Session CRUD |
| `UserProfileRepository.cs` | `UserProfileRepository` | User profile upsert/read |
| `ProgressRepository.cs` | `ProgressRepository` | Stage progress batch read/write |
| `CurrencyRepository.cs` | `CurrencyRepository` | Soft currency grant/deduct with FOR UPDATE + audit log |
| `StaminaRepository.cs` | `StaminaRepository` | Stamina read/deduct/add with lazy recharge and FOR UPDATE |
| `InventoryRepository.cs` | `InventoryRepository` | Inventory grant/deduct |
| `RankingRepository.cs` | `RankingRepository` | Best records + ranking cache CRUD |
| `DailyChallengeRepository.cs` | `DailyChallengeRepository` | Daily challenge read + atomic play count increment |
| `PlayerSettingsRepository.cs` | `PlayerSettingsRepository` | Player settings get-or-default + upsert |
| `StageEndTransactionRepository.cs` | `StageEndTransactionRepository` | Atomic stage-end TX (progress, best, stamina, currency, ranking + daily increment) |
| `StaminaRefillTransactionRepository.cs` | `StaminaRefillTransactionRepository` | Atomic full refill TX (stamina+currency+log) |
| `DailyChallengeCompleteTransactionRepository.cs` | `DailyChallengeCompleteTransactionRepository` | Atomic challenge complete TX (progress+streak+rewards) |
| `ShopPurchaseTransactionRepository.cs` | `ShopPurchaseTransactionRepository` | Atomic shop purchase TX (currency+inventory) |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `StageEndTransactionRepository.ExecuteAsync` | method | First-clear soft reward, clear stamina refund, ranking cache update; FOR UPDATE NOWAIT on stage rows |
| `StaminaRefillTransactionRepository.ExecuteAsync` | method | FOR UPDATE on stamina_state + user_currency; throws if full or insufficient funds |
| `DailyChallengeCompleteTransactionRepository.ExecuteAsync` | method | FOR UPDATE NOWAIT on daily_challenge_progress; throws if completed or not enough plays |
| `DailyChallengeRepository.IncrementPlayCountAsync` | method | INSERT ON CONFLICT DO UPDATE play_count+1; always increments (even if completed) |

## Cross-refs
- Depends on: `Domain.Entities.*`, `Domain.Interfaces.*`
- Consumed by: `Application.*Service` (via injected interfaces)

## Rules
- EF Core is ORM only — never run migrations; schema managed via `npm run gen:orm`
- Transaction repos use `BeginTransactionAsync` → `SaveChangesAsync` → `CommitAsync`
- Lock order in StageEndTransaction: stage_progress → stage_best_records → stamina_state → user_currency → user_ranking_cache (prevents deadlocks)
- `ChangeTracker.Clear()` before reads that follow `ExecuteSqlInterpolatedAsync` (avoids stale cache)
- PostgreSQL exception 55P03 (lock_not_available) bubbles up from FOR UPDATE NOWAIT — GlobalExceptionMiddleware maps it to 409
