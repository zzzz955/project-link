# Core — App lifecycle, singletons, state machine, popups

## Files
| file | class | role |
|---|---|---|
| `BootstrapEntry.cs` | `BootstrapEntry` | App entry; instantiates all manager singletons, loads Title scene |
| `GameContext.cs` | `GameContext` | Static cross-scene state (selected stage ID) |
| `GameState.cs` | `GameState` | Enum: Idle, Drawing, Completed |
| `GameStateMachine.cs` | `GameStateMachine` | Pure FSM with validated transitions + change event |
| `InGameController.cs` | `InGameController` | Singleton; orchestrates board/input/paths/HUD/timer |
| `UIManager.cs` | `UIManager` | Canvas layer resolver (Background/HUD/Popup/System) |
| `PopupManager.cs` | `PopupManager` | Event-driven popup request router + stack lifecycle; back-press aware |
| `SceneLoader.cs` | `SceneLoader` | Faded async scene transitions |
| `DataManager.cs` | `DataManager` | PlayerPrefs persistence: progress, settings |
| `SoundManager.cs` | `SoundManager` | BGM + SFX AudioSource management |
| `HapticManager.cs` | `HapticManager` | Platform haptic feedback (static helpers) |
| `AppEnvironment.cs` | `AppEnvironment`, `AppConfig` | Env enum (Dev/Prod) + URL constants; prod URLs are placeholders until deployment |
| `IAuthService.cs` | `IAuthService` | Auth abstraction: EnsureAuth, Refresh, GetToken, SetToken, ClearToken |
| `MockAuthService.cs` | `MockAuthService`, `MockAuthScenario` | Mock IAuthService; scenario-driven (Success / Failure / SessionExpired); no HTTP |
| `NetworkManager.cs` | `NetworkManager` | HTTP GET/POST/PATCH coroutines with client/protocol/auth headers; delegates auth to IAuthService; clears token on 401 |
| `PoolManager.cs` | `PoolManager` | Keyed GameObject pool |
| `LocalizationManager.cs` | `LocalizationManager` | String table + language switching; fires LanguageChanged |
| `FontRegistry.cs` | `FontRegistry` | ScriptableObject: LanguageCode → TMP_FontAsset pair |

## Symbols
| symbol | kind | note |
|---|---|---|
| `GameContext.SelectedStageId` | prop | set before `SceneLoader.LoadScene("Game")` |
| `GameContext.SetStageSession(string,int,int)` | method | stores server stage session token, move limit, time limit, start timestamp |
| `GameContext.ClearStageSession()` | method | clears server stage-session state |
| `GameState` | enum | Idle \| Drawing \| Completed |
| `GameStateMachine.Current` | prop | read-only current state |
| `GameStateMachine.TryTransition(GameState)` | method | returns false if transition invalid |
| `GameStateMachine.OnStateChanged` | event | `Action<GameState,GameState>` (from, to) |
| `InGameController.Instance` | prop | singleton; valid during Game scene lifetime |
| `InGameController.SetInputEnabled(bool)` | method | enables/disables TouchInputHandler |
| `InGameController._pathViewMap` | field | `Dictionary<PathModel,PathView>`; one PathView per PathModel |
| `InGameController._activeGroupId` | field | groupId of the currently-drawing path |
| `InGameController.EnsurePathViews(int)` | method | creates PathView for any new PathModel in the group |
| `InGameController.RefreshGroupViews(int)` | method | refreshes all PathViews for a given groupId |
| `InGameController.GetConnectedCount()` | method | counts groups where all nodes are endpoints of complete paths (uses PathValidator.IsGroupConnected) |
| `UIManager.GetLayer(UILayer)` | method | returns canvas Transform for named layer |
| `PopupId` | enum | popup request ids: ReturnTitle, ExitGame, Settings, BuyItem, Energy, DailyChallenge, Account, Reward, StageClear |
| `PopupRequest` | struct | event payload: PopupId + optional object payload |
| `PopupManager.Request(PopupId,object)` | method | static event-driven popup request entry point |
| `PopupManager.Open<T>()` | method | code-instantiates legacy in-game popup T on Popup layer, pushes stack |
| `PopupManager.CloseTop()` | method | destroys top popup, re-shows previous |
| `PopupManager.HasPopup` | prop | true if any popup on stack |
| `AppEnvironment` | enum | `Dev` \| `Prod` |
| `AppConfig.DevGameServerUrl` | const | `http://localhost:20101` |
| `AppConfig.ProdGameServerUrl` | const | placeholder — update when prod server is deployed |
| `AppConfig.DevPlatformAuthUrl` | const | `http://localhost:20001` |
| `AppConfig.ProdPlatformAuthUrl` | const | placeholder — update when prod auth is deployed |
| `IAuthService.EnsureAuth(Action<bool,string>)` | method | ensures a valid token exists (guest login if needed); delegates to active IAuthService |
| `IAuthService.Refresh(Action<bool,string>)` | method | refreshes the current token; scenario-aware in MockAuthService |
| `IAuthService.GetToken()` | method | returns current Bearer token |
| `MockAuthScenario` | enum | `Success` \| `Failure` \| `SessionExpired` — controls MockAuthService behavior |
| `MockAuthService.Scenario` | prop | set to change active auth scenario for UI/flow testing |
| `NetworkManager.AuthService` | prop | get/set active IAuthService; defaults to MockAuthService on Awake |
| `NetworkManager.SetAuthToken(string)` | method | delegates to IAuthService.SetToken |
| `NetworkManager.EnsureGuestAuth(Action<bool,string>)` | method | delegates to IAuthService.EnsureAuth |
| `NetworkManager.ClearAuthToken()` | method | delegates to IAuthService.ClearToken |
| `NetworkManager.Get(string,Action<bool,string>)` | method | sends GET with required version/protocol headers |
| `NetworkManager.Post(string,string,Action<bool,string>)` | method | sends JSON POST with required version/protocol/auth headers |
| `NetworkManager.Patch(string,string,Action<bool,string>)` | method | sends JSON PATCH with required version/protocol/auth headers |
| `SceneLoader.LoadScene(string)` | method | fade-out → load → fade-in |
| `DataManager.ClearStage(int,int)` | method | records clear + star rating |
| `DataManager.IsStageUnlocked(int)` | method | stage 1 always unlocked; others need prev clear |
| `DataManager.HapticEnabled` | prop | persisted toggle |
| `LocalizationManager.Get(string)` | method | static; stringId → localized string (EN fallback) |
| `LocalizationManager.SetLanguage(LanguageCode)` | method | static; persists + fires LanguageChanged |
| `LocalizationManager.LanguageChanged` | event | fired after language change; `LocalizedText` subscribes |
| `HapticManager.PlayConnected()` | method | static; path-completion haptic |
| `HapticManager.PlayBlocked()` | method | static; blocked-move haptic |
| `HapticManager.PlayErased()` | method | static; erase-complete haptic |
| `PoolManager.Get(string)` | method | dequeues or instantiates from registered pool |
| `PoolManager.Return(string,GameObject)` | method | re-enqueues object |
| `FontRegistry.Instance` | prop | loaded via `Resources.Load("FontRegistry")` |
| `FontRegistry.TryGetFonts(LanguageCode,...)` | method | out regular + bold TMP_FontAsset |

## Cross-refs
- Consumed by: all scenes (InGame, OutGame) — singleton managers accessed globally via static Instance
- Depends on: client `Data.StageLoader` (InGameController loads StageData at scene start, GameContext holds selected stageId)

## Rules
- All singletons pattern: `if (Instance != null) { Destroy(gameObject); return; }` in Awake
- Popup T must extend `PopupBase`; use `UILayer.Popup` for all overlays
- New UI popups should be requested through `PopupManager.Request(PopupId, payload)` and loaded from `Resources/Prefabs/UI`.
- FSM valid transitions only: Idle↔Drawing, Drawing→Completed
- `InGameController` calls `ColorPalette.Init(stageData.NodeColors)` and `BoardCameraController.Init` in Start()
- `InGameController` wires OnTimeUp → HandleTimeUp (cancels drawing, shows TimeoutPopup)
- Pause popup must call `_timer.Pause()` / `_timer.Resume()` around show/dismiss
- `InGameController` uses `_board.GroupIds` (not ColorIds) and `stageData.TimeLimit` (not stageData.Info.timeLimit)
