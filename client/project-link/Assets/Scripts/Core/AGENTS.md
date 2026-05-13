# Core - App lifecycle, singletons, state machine, popups

## Files
| file | class | role |
|---|---|---|
| `BootstrapEntry.cs` | `BootstrapEntry` | App entry; disables info-log stack traces and instantiates manager singletons before Bootstrap UI drives scene transition |
| `GameContext.cs` | `GameContext` | Static cross-scene state (selected stage ID and active stage session) |
| `GameState.cs` | `GameState` | Enum: Idle, Drawing, Completed |
| `GameStateMachine.cs` | `GameStateMachine` | Pure FSM with validated transitions + change event |
| `InGameController.cs` | `InGameController` | Singleton; orchestrates board/input/paths/HUD/timer |
| `UIManager.cs` | `UIManager` | Canvas layer resolver (Background/HUD/Popup/System) |
| `PopupManager.cs` | `PopupManager` | Event-driven popup request router + stack lifecycle; back-press aware |
| `SceneLoader.cs` | `SceneLoader` | Faded async scene transitions |
| `DataManager.cs` | `DataManager` | PlayerPrefs persistence: progress, settings |
| `SoundManager.cs` | `SoundManager` | BGM + SFX AudioSource management |
| `HapticManager.cs` | `HapticManager` | Platform haptic feedback (static helpers) |
| `AppEnvironment.cs` | `AppEnvironment`, `AppConfig` | Env enum (Dev/Prod) + game/auth URL constants |
| `IAuthService.cs` | `IAuthService` | Auth abstraction: guest/social login, refresh, logout, token persistence |
| `ITokenStorage.cs` | `ITokenStorage` | Token persistence abstraction; Dev=PlayerPrefs, Prod=AES-encrypted PlayerPrefs |
| `PlayerPrefsTokenStorage.cs` | `PlayerPrefsTokenStorage` | Dev token storage backed by PlayerPrefs |
| `SecureTokenStorage.cs` | `SecureTokenStorage` | Prod token storage: AES-encrypted PlayerPrefs using device ID as key |
| `PlatformAuthService.cs` | `PlatformAuthService` | Real platform auth HTTP adapter for guest/google/refresh/logout; uses ITokenStorage |
| `UiEventBus.cs` | `UiEventBus` | Typed event bus for UI busy/error/viewmodel/auth events |
| `ToastPresenter.cs` | `ToastPresenter` | Global top-stack toast renderer subscribed to `UiErrorRaised` |
| `NetworkManager.cs` | `NetworkManager` | HTTP GET/POST/PATCH coroutines with client/protocol/auth headers; delegates auth to IAuthService; clears token on 401 |
| `PoolManager.cs` | `PoolManager` | Keyed GameObject pool |
| `LocalizationManager.cs` | `LocalizationManager` | String/error table + language switching; fires LanguageChanged |
| `FontRegistry.cs` | `FontRegistry` | ScriptableObject: LanguageCode -> TMP_FontAsset pair |

## Symbols
| symbol | kind | note |
|---|---|---|
| `GameContext.SelectedStageId` | prop | set before `SceneLoader.LoadScene("Game")` |
| `GameContext.SetStageSession(string,int,int)` | method | stores server stage session token, move limit, time limit, start timestamp |
| `GameContext.ClearStageSession()` | method | clears server stage-session state |
| `GameContext.SetDailyChallengeRun(...)` | method | stores active daily challenge stage sequence for clear-popup next routing |
| `GameContext.TryGetNextDailyChallengeStage(out int)` | method | returns the next daily challenge stage instead of normal campaign next |
| `GameContext.ClearDailyChallengeRun()` | method | clears daily challenge routing state on Lobby/Title return |
| `GameContext.SuppressNextTitleSilentLogin()` | method | one-shot guard for intentional Lobby -> Title navigation |
| `GameContext.ConsumeTitleSilentLoginSuppression()` | method | Title entry consumes the one-shot silent-login guard |
| `GameStateMachine.TryTransition(GameState)` | method | returns false if transition invalid |
| `GameStateMachine.OnStateChanged` | event | `Action<GameState,GameState>` (from, to) |
| `InGameController.Instance` | prop | singleton; valid during Game scene lifetime |
| `InGameController.OpenPausePopup()` | method | opens PausePopup with timer pause/resume callbacks |
| `InGameController.AbandonStageAndLoad(string)` | method | submits stage fail with active session token, clears context, then loads target scene |
| `InGameController.HandleStageStarted(...)` | method | enables gameplay only after server stage-start success; insufficient stamina opens Energy popup and returns Lobby |
| `UIManager.GetLayer(UILayer)` | method | returns canvas Transform for named layer |
| `PopupId` | enum | popup ids: ReturnTitle, ExitGame, Settings, BuyItem, Energy, DailyChallenge, Account, Reward, StageClear, SessionExpired, Pause, ForceUpdate, Maintenance, StageDetail |
| `PopupManager.Request(PopupId,object)` | method | static event-driven popup request entry point |
| `PopupManager.Open<T>()` | method | code-instantiates legacy popup T on Popup layer, pushes stack |
| `PopupManager.CloseTop()` | method | destroys top popup, re-shows previous |
| `PopupBase.BindOverlayClose()` | method | binds the first child Button named "Overlay" to CloseTop; call from each prefab popup Init() |
| `AppEnvironment` | enum | `Dev` / `Prod` |
| `AppConfig.DevGameServerUrl` | const | `http://localhost:20101` |
| `AppConfig.DevPlatformAuthUrl` | const | `http://localhost:20001` — server-side only; client routes auth via game server |
| `IAuthService.EnsureAuth(Action<bool,string>)` | method | ensures a valid access token exists; refreshes or guest-logins through active auth service |
| `IAuthService.LoginGuest(Action<bool,string>)` | method | creates/restores platform guest session |
| `IAuthService.LoginGoogle(string,string,Action<bool,string>)` | method | submits native Google ID token to platform auth |
| `IAuthService.Refresh(Action<bool,string>)` | method | refreshes access/refresh token pair through platform auth |
| `IAuthService.Logout(Action<bool,string>)` | method | revokes local platform refresh session |
| `PlatformAuthService.HasStoredSession` | prop | true when a platform refresh token is stored locally |
| `PlatformAuthService.Provider` | prop | last successful provider (`guest`, `google`, or refresh fallback) |
| `SecureTokenStorage` | class | AES-256 encrypts values before writing to PlayerPrefs; key = SHA-256(deviceUniqueIdentifier + ":project-link") |
| `PlatformAuthService.httpLogging` | field | ctor param `bool httpLogging`; toggles `[AUTH]` console logs; all levels include request/response fields, Info hides payload values |
| `UiEventBus.Publish<T>(T)` | method | publishes typed UI/auth events to scene controllers |
| `UiBusyChanged` | struct | event payload for async API/loading state |
| `UiErrorRaised` | struct | event payload for localized toast/popup error rendering |
| `UiViewModelChanged` | struct | event payload for viewmodel-to-render notifications |
| `AuthStateChanged` | struct | event payload for token/provider changes |
| `ToastPresenter.OnError(UiErrorRaised)` | method | ignores blocking errors and renders localized non-critical errors as top toasts |
| `NetworkManager.AuthService` | prop | active IAuthService; defaults to `PlatformAuthService` |
| `NetworkManager.LoginGuest(Action<bool,string>)` | method | delegates explicit guest login to IAuthService |
| `NetworkManager.LoginGoogle(string,string,Action<bool,string>)` | method | delegates Google token exchange to IAuthService |
| `NetworkManager.RefreshAuth(Action<bool,string>)` | method | delegates refresh to IAuthService |
| `NetworkManager.EnsureGuestAuth(Action<bool,string>)` | method | delegates to IAuthService.EnsureAuth |
| `NetworkManager.Get(string,Action<bool,string>)` | method | sends GET with required version/protocol/auth headers |
| `NetworkManager.Post(string,string,Action<bool,string>)` | method | sends JSON POST with required version/protocol/auth headers |
| `NetworkManager.Patch(string,string,Action<bool,string>)` | method | sends JSON PATCH with required version/protocol/auth headers |
| `NetworkManager.httpLogging` | field | `[SerializeField] bool`; toggles `[HTTP]` console logs; all levels include request/response fields, Info hides payload values |
| `LocalizationManager.Get(string)` | method | static stringId -> localized string (EN fallback) |
| `LocalizationManager.GetError(string)` | method | static errorCode -> localized error message (EN fallback) |
| `LocalizationManager.SetLanguage(LanguageCode)` | method | persists + fires LanguageChanged |
| `PoolManager.Get(string)` | method | dequeues or instantiates from registered pool |
| `PoolManager.Return(string,GameObject)` | method | re-enqueues object |
| `FontRegistry.TryGetFonts(LanguageCode,...)` | method | out regular + bold TMP_FontAsset |

## Cross-refs
- Consumed by: all scenes (InGame, OutGame) via singleton/static access
- Depends on: client `Data.StageLoader`, `shared/datas/string/`

## Rules
- All singletons pattern: `if (Instance != null) { Destroy(gameObject); return; }` in Awake.
- Popup T must extend `PopupBase`; use `UILayer.Popup` for all overlays.
- New UI popups should be requested through `PopupManager.Request(PopupId, payload)` and loaded from `Resources/Prefabs/UI`.
- `NetworkManager` owns Dev/Prod game/auth base URL selection through `AppEnvironment`.
- FSM valid transitions only: Idle -> Drawing, Drawing -> Completed.
- Pause popup must call `_timer.Pause()` / `_timer.Resume()` around show/dismiss.
