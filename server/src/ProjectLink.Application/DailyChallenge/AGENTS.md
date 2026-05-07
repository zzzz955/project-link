# ProjectLink.Application/DailyChallenge

## Files
| file | class | role |
|------|-------|------|
| `DailyChallengeService.cs` | `DailyChallengeService` | Daily challenge state query and completion |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `DailyChallengeService.GetAsync` | method | Returns challenge state, 7-tile streak display, and today's reward preview |
| `DailyChallengeService.CompleteAsync` | method | Validates completability, computes streak/reward, calls `IDailyChallengeCompleteTransaction` |
| `DailyChallengeService.GetCurrentStreakAsync` | method | private; reads yesterday's row to derive running streak (today's row streak_days=0 until completed) |

## Cross-refs
- Consumed by: `API.Controllers.DailyChallengeController` → `GET /api/daily-challenge`, `POST /api/daily-challenge/complete`
- Depends on: `IDailyChallengeRepository`, `IDailyChallengeCompleteTransaction`, `IStaticDataService`

## Rules
- Streak is always derived from yesterday's completed row, NOT today's row (which starts at streak_days=0)
- `todayStreakDay = (currentStreak % 7) + 1` → maps to `outgame_daily_reward.streakDay` (1..7, cycles every 7 days)
- Completion validation order: row exists → play_count >= target → !completed
