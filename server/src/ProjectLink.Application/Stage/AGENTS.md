# ProjectLink.Application/Stage

## Files
| file | class | role |
|------|-------|------|
| `StageService.cs` | `StageService` | Stage lifecycle: start (spend stamina + issue session), lock (freeze items), end (score + persist), extend (refund stamina) |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `StageService.StartAsync` | method | Deducts stamina, replaces any active session with a new paid attempt, returns `StageStartResponse` with item counts from static data |
| `StageService.LockAsync` | method | Validates `sessionToken`, transitions session to locked state; must be called before EndAsync |
| `StageService.EndAsync` | method | Validates sessionToken + move limit; delegates first-clear reward + clear stamina refund to `IStageEndTransaction`; calls `RankingService.OnStageEndAsync` on best record; calls `StreakChallengeService.ProcessStageResultAsync`; elapsed-ms tolerance uses `_rankingService.NetworkToleranceMs` |
| `StageService.ExtendAsync` | method | Extends session TTL; validates sessionToken; deducts soft currency; works in both setup phase and in-game phase; cost from `IStaticDataService.GetTimeExtendConfig` |

## Cross-refs
- Depends on: `IStageSessionCache`, `IStaminaRepository`, `IInventoryRepository`, `IStageEndTransaction`, `IStaticDataService`, `RankingService`, `StreakChallengeService`
- Consumed by: `API.Controllers.StageController` → `POST /api/stage/{id}/start`, `POST /api/stage/{id}/lock`, `POST /api/stage/{id}/end`, `POST /api/stage/{id}/extend`

## Rules
- StartAsync treats any active session as abandoned; a new start costs stamina and overwrites the cached token.
- `EndAsync`: move-limit check: `if (stageData.MoveLimit > 0 && movesUsed > stageData.MoveLimit) throw InvalidStageResultException`
- Session token validated in every mutating call — token mismatch throws `StageSessionNotFoundException`
- Stamina deduction and session creation are NOT in the same transaction (stamina repo → session cache); session failure after stamina deduction is acceptable (idempotent retry)
