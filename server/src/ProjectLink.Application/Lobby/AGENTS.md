# ProjectLink.Application/Lobby

## Files
| file | class | role |
|------|-------|------|
| `LobbyService.cs` | `LobbyService` | Aggregates profile+stamina+currency+progress+daily+event into one response |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `LobbyService.GetAsync` | method | 5 sequential DB reads + 1 conditional yesterday streak read |

## Cross-refs
- Consumed by: `API.Controllers.LobbyController` → `GET /api/lobby`
- Depends on: `IUserProfileRepository`, `IStaminaRepository`, `ICurrencyRepository`, `IProgressRepository`, `IDailyChallengeRepository`, `IStaticDataService`

## Rules
- Streak is derived from yesterday's row if today's is not yet completed (streak_days on today's row is 0 until CompleteAsync)
- Season event IsActive checked against current UTC time vs event StartAt/EndAt
