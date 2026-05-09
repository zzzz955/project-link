# ProjectLink.Application/DailyChallenge

## Files
| file | class | role |
|------|-------|------|
| `DailyChallengeService.cs` | `DailyChallengeService` | Daily challenge state query and completion |
| `DailyChallengeStageSelector.cs` | `DailyChallengeStageSelector` | internal static; date-seeded stage selection used by DailyChallengeService and StageService |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `DailyChallengeService.GetAsync` | method | Returns challenge state including `TodayStageIds` (date-seeded), 7-tile streak display, and reward preview |
| `DailyChallengeStageSelector.GetTodayStageIds` | method | internal static; `seed = DateOnly.DayNumber`; returns N stage IDs deterministically from allStages |
| `DailyChallengeService.CompleteAsync` | method | Validates completability, computes streak/reward, calls `IDailyChallengeCompleteTransaction` |
| `DailyChallengeService.GetCurrentStreakAsync` | method | private; reads yesterday's row to derive running streak (today's row streak_days=0 until completed) |

## Cross-refs
- Consumed by: `API.Controllers.DailyChallengeController` → `GET /api/daily-challenge`, `POST /api/daily-challenge/complete`
- Consumed by: `Application.Stage.StageService` → imports `DailyChallengeStageSelector.GetTodayStageIds` to set `IsDailyChallengeStage` on `StageEndDbCommand`
- Depends on: `IDailyChallengeRepository`, `IDailyChallengeCompleteTransaction`, `IStaticDataService`

## Rules
- Design: N stages selected per day via date-seeded RNG (`seed = DateOnly.DayNumber`); same stages for all players on the same day
- N = `OutgameDailyChallengeData.StagePickCount` from `outgame_daily_challenge.csv` (default 3)
- No dedicated start route — client uses existing `POST /api/stage/{stageId}/start`; `StageService.End` increments play_count via `IStageEndTransaction` when `IsDailyChallengeStage = true`
- Streak is always derived from yesterday's completed row, NOT today's row (which starts at streak_days=0)
- `todayStreakDay = (currentStreak % 7) + 1` → maps to `outgame_daily_reward.streakDay` (1..7, cycles every 7 days)
- Completion validation order: row exists → play_count >= target → !completed
