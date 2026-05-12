# ProjectLink.Application/Ranking

## Files
| file | class | role |
|------|-------|------|
| `RankingService.cs` | `RankingService` | Redis-backed leaderboard: build list responses and post-stage Redis updates |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `RankingService.OnStageEndAsync` | method | Called by StageService after stage end; updates Redis key `ranking:stage:{id}:score` (descending score) from `StageEndDbResult.Score`; only called when `IsBestRecord == true` |
| `RankingService.OnStageClearAsync` | method | Updates `stage_best_records.BestScore`; best-record check: `existing.BestScore >= score` |
| `RankingService.GetStageRankingAsync` | method | Returns top-10 for `ranking:stage:{id}:score`; category=`"STAGE_SCORE"`; MetricLabel=`"Best Score"`; order descending |
| `RankingService.BuildListResponseAsync` | method | Builds `RankingListResponse` from Redis ZRANGE; includes `AvatarId`, `IsMe` per entry, `Category` and `MetricLabel` on response |
| `RankingService.GetMyRankAsync` | method | Returns `MyRankResponse` with stagesCleared and totalScore ranks from Redis |
| `RankingService.RebuildFromDbAsync` | method | Cold-start: rebuilds Redis sorted sets from DB (called on server startup); uses `:score` key with `EncodeDescending` |
| `RankingService.NetworkToleranceMs` | property | `public int`; consumed by `StageService.EndAsync` for elapsed-ms tolerance check |

## Cross-refs
- Depends on: `IConnectionMultiplexer` (Redis), `IUserRepository`, `IStaticDataService`
- Consumed by: `Application.StageService.EndAsync` (via `OnStageEndAsync`)
- Consumed by: `API.Controllers.RankingController` → `GET /api/ranking/{category}`, `GET /api/ranking/me`

## Rules
- Redis updates are best-effort after DB commit; recoverable via `RebuildFromDbAsync`
- `OnStageEndAsync` uses totalScore and stagesCleared from `StageEndDbResult` — no second DB read
- Stage ranking uses descending score (higher score = better rank); Redis key is `ranking:stage:{id}:score`
- Lock order within StageEndTransaction: stage_progress → stage_best_records → user_currency → user_ranking_cache
