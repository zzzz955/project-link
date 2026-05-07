# ProjectLink.Application/Stage

## Files
| file | class | role |
|------|-------|------|
| `StageService.cs` | `StageService` | Stage lifecycle: start (spend stamina + issue session), lock (freeze items), end (score + persist), extend (refund stamina) |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `StageService.StartAsync` | method | Deducts stamina, creates session, returns `StageStartResponse` with item counts from static data |
| `StageService.LockAsync` | method | Validates `sessionToken`, transitions session to locked state; must be called before EndAsync |
| `StageService.EndAsync` | method | Validates sessionToken + move limit; delegates to `IStageEndTransaction`; calls `RankingService.OnStageEndAsync` only on best record |
| `StageService.ExtendAsync` | method | Refunds stamina on stage-fail; validates sessionToken; cost from `IStaticDataService.GetStaminaConfig()` |

## Cross-refs
- Depends on: `ISessionCache`, `IStaminaRepository`, `IInventoryRepository`, `IStageEndTransaction`, `IStaticDataService`, `RankingService`
- Consumed by: `API.Controllers.StageController` → `POST /api/stage/{id}/start`, `POST /api/stage/{id}/lock`, `POST /api/stage/{id}/end`, `POST /api/stage/{id}/extend`

## Rules
- `EndAsync`: move-limit check: `if (stageData.MoveLimit > 0 && movesUsed > stageData.MoveLimit) throw InvalidStageResultException`
- Session token validated in every mutating call — token mismatch throws `StageSessionNotFoundException`
- Stamina deduction and session creation are NOT in the same transaction (stamina repo → session cache); session failure after stamina deduction is acceptable (idempotent retry)
