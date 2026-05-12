# InGame/UI - HUD, timer display, in-game popups, circular gauge

## Files
| file | class | role |
|---|---|---|
| `InGameHUD.cs` | `InGameHUD` | Runtime wireframe HUD slots + transparent tool hotspots |
| `GameWireframeController.cs` | `GameWireframeController` | Game generated UI refs; Inspector-assignable shell buttons/labels |
| `CircularGauge.cs` | `CircularGauge` | LineRenderer arc progress indicator (erase mode) |
| `ClearPopup.cs` | `ClearPopup`, `StageClearPopupModel` | Prefab-compatible stage-clear overlay with server reward/progress fields |
| `PausePopup.cs` | `PausePopup` | Pause overlay: Resume/Retry/Lobby |
| `TimeoutPopup.cs` | `TimeoutPopup` | Time-expired overlay: Retry/Lobby; no back-press dismiss |
| `HapticManager.cs` | `HapticManager` | Thin shim forwarding calls to `Core.HapticManager` |

## Symbols
| symbol | kind | note |
|---|---|---|
| `InGameHUD.Init(int,int,Func<int>,int)` | method | stageId, totalColors, connectedCount getter, timeLimitSeconds (0=no timer); rebuilds wireframe HUD |
| `InGameHUD.Refresh()` | method | updates objective counter text via connectedCount getter |
| `InGameHUD.SetTimerDisplay(float)` | method | remaining seconds -> `MM:SS` (zero-padded); color -> urgent red when <= 10 s |
| `InGameHUD.SetMoveDisplay(int,int)` | method | moves used / server move limit text |
| `GameWireframeController.SetToolButtonsInteractable(bool)` | method | toggles generated shell button refs |
| `CircularGauge.Show(Vector3,Color)` | method | positions at world pos, sets arc color, enables |
| `CircularGauge.SetProgress(float)` | method | t in [0,1] draws arc (0=empty, 1=full circle) |
| `CircularGauge.Hide()` | method | disables gauge |
| `ClearPopup.Init(StageClearPopupModel)` | method | server-backed stage clear model; binds Next/Retry/Lobby buttons |
| `PausePopup.Init(Action)` | method | onResume callback; back-press resumes; Retry/Lobby abandon current stage before navigation |
| `TimeoutPopup.Init(int)` | method | stageId only; Retry/Lobby abandon current stage before navigation; OnBackPressed() is no-op |

## Cross-refs
- Consumed by: client `Core.InGameController` (show/hide popups, call InGameHUD.Init/Refresh/SetTimerDisplay)
- Depends on: client `InGame.StageTimer` (timer remaining seconds → InGameHUD.SetTimerDisplay)
- Depends on: `shared/datas/string/` (localized strings via `Utils.LocalizedText` in popup labels)

## Rules
- Timer text format: always `MM:SS` zero-padded (e.g. `01:30`, `00:09`).
- Timer urgency threshold: <= 10 s switches to `_timerUrgent` red.
- Tool purchase popup is requested through `PopupManager.Request(PopupId.BuyItem)`.
- `HapticManager` here is a shim only; logic lives in `Core.HapticManager`.
- `InGameHUD` is created at runtime (no prefab); parented to `UILayer.HUD`.
