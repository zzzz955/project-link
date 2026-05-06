# InGame — In-game gameplay domain

## Nav
| path | role |
|---|---|
| `Board/` | Grid data model + cell rendering → `Board/AGENTS.md` |
| `Path/` | Path drawing, validation, line rendering → `Path/AGENTS.md` |
| `Input/` | Touch input, longpress detection, erase trigger → `Input/AGENTS.md` |
| `UI/` | HUD, timer display, popups, circular gauge → `UI/AGENTS.md` |

## Files (directly in InGame/)
| file | class | role |
|---|---|---|
| `StageTimer.cs` | `StageTimer` | Anti-tamper countdown; dual-source (realtimeSinceStartup + DateTime.UtcNow) |

## Symbols
| symbol | kind | note |
|---|---|---|
| `StageTimer.Start(float)` | method | begins countdown; timeLimitSeconds=0 → no-op |
| `StageTimer.Tick()` | method | call every Update; fires OnTimeUp when expired |
| `StageTimer.Pause()` / `Resume()` | methods | pause offset tracking for popup suspension |
| `StageTimer.Remaining` | prop | seconds left; float.MaxValue if no limit |
| `StageTimer.HasLimit` | prop | false when timeLimit == 0 |
| `StageTimer.IsExpired` | prop | true after OnTimeUp has fired |
| `StageTimer.OnTimeUp` | event | `Action`; fired once when elapsed ≥ timeLimit |

## Rules
- Namespace: `ProjectLink.InGame.[SubDir]` mirrors folder structure
- `StageTimer` is a pure C# class (no MonoBehaviour); tick via `InGameController.Update()`
- Tamper guard: if realtimeSinceStartup vs UtcNow diverge > 2s, take the larger value
