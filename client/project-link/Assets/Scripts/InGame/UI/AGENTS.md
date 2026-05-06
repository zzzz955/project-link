# InGame/UI — HUD, timer display, in-game popups, circular gauge

## Files
| file | class | role |
|---|---|---|
| `InGameHUD.cs` | `InGameHUD` | Top bar: pipe counter + stage label + optional timer |
| `CircularGauge.cs` | `CircularGauge` | LineRenderer arc progress indicator (erase mode) |
| `ClearPopup.cs` | `ClearPopup` | Stage-clear overlay: star display + Next/Retry/Lobby |
| `PausePopup.cs` | `PausePopup` | Pause overlay: Resume/Retry/Lobby |
| `TimeoutPopup.cs` | `TimeoutPopup` | Time-expired overlay: Retry/Lobby; no back-press dismiss |
| `HapticManager.cs` | `HapticManager` | Thin shim forwarding calls to `Core.HapticManager` |

## Symbols
| symbol | kind | note |
|---|---|---|
| `InGameHUD.Init(int,int,Func<int>,int)` | method | stageId, totalColors, connectedCount getter, timeLimitSeconds (0=no timer) |
| `InGameHUD.Refresh()` | method | updates pipe counter text via connectedCount getter |
| `InGameHUD.SetTimerDisplay(float)` | method | remaining seconds → "MM:SS" (zero-padded); color → urgent red when ≤10 s |
| `CircularGauge.Show(Vector3,Color)` | method | positions at world pos, sets arc color, enables |
| `CircularGauge.SetProgress(float)` | method | t ∈ [0,1] draws arc (0=empty, 1=full circle) |
| `CircularGauge.Hide()` | method | disables gauge |
| `ClearPopup.Init(int,int)` | method | stageId, stars (0-3) |
| `PausePopup.Init(Action)` | method | onResume callback; back-press → resume |
| `TimeoutPopup.Init(int)` | method | stageId only; OnBackPressed() is no-op |

## Rules
- Timer text format: always `MM:SS` zero-padded (e.g. "01:30", "00:09")
- Timer urgency threshold: ≤10 s → color switches to `_timerUrgent` (red)
- All popups use `PopupManager.Open<T>()` → placed on `UILayer.Popup`
- `HapticManager` here is a shim only — logic lives in `Core.HapticManager`
- `InGameHUD` is created at runtime (no prefab); parented to `UILayer.HUD`
