# ProjectLink.Application/Ranking

## Files
| file | class | role |
|------|-------|------|
| `RankingService.cs` | `RankingService` | Redis-backed leaderboard: build list responses and post-stage Redis updates |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `RankingService.OnStageEndAsync` | method | Called by StageService after stage end; updates Redis ZADD for stagesCleared and totalScore using pre-computed values from `StageEndDbResult`; only called when `IsBestRecord == true` |
| `RankingService.BuildListResponseAsync` | method | Builds `RankingListResponse` from Redis ZRANGE; includes `AvatarId`, `IsMe` per entry, `Category` and `MetricLabel` on response |
| `RankingService.GetMyRankAsync` | method | Returns `MyRankResponse` with stagesCleared and totalScore ranks from Redis |
| `RankingService.RebuildFromDbAsync` | method | Cold-start: rebuilds Redis sorted sets from DB (called on server startup) |

## Cross-refs
- Depends on: `IConnectionMultiplexer` (Redis), `IUserRepository`, `IStaticDataService`
- Consumed by: `Application.StageService.EndAsync` (via `OnStageEndAsync`)
- Consumed by: `API.Controllers.RankingController` → `GET /api/ranking/{category}`, `GET /api/ranking/me`

## Rules
- Redis updates are best-effort after DB commit; recoverable via `RebuildFromDbAsync`
- `OnStageEndAsync` uses totalScore and stagesCleared from `StageEndDbResult` — no second DB read
- Lock order within StageEndTransaction: stage_progress → stage_best_records → user_currency → user_ranking_cache
