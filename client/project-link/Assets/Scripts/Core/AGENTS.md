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
| `PopupManager.cs` | `PopupManager`, `PopupBase` | Event-driven popup request router + stack lifecycle; `PopupBase.Awake/Start` auto-plays panel scale-in (EaseOutBack 0.22 s) and adds `ButtonPressEffect` to all non-Overlay child buttons |
| `ButtonPressEffect.cs` | `ButtonPressEffect` | MonoBehaviour; scale-down (0.88) from current localScale on pointer down, restore original Vector3 on up/exit; auto-added by `PopupBase` to all popup buttons |
| `SceneLoader.cs` | `SceneLoader` | Faded async scene transitions; supports overlay hold via HoldForReady/NotifyReady |
| `DataManager.cs` | `DataManager` | PlayerPrefs persistence: progress, settings |
| `SoundManager.cs` | `SoundManager` | BGM + SFX AudioSource management |
| `HapticManager.cs` | `HapticManager` | Platform haptic feedback (static helpers) |
| `AppEnvironment.cs` | `AppEnvironment`, `AppConfig` | Env enum (Dev/Prod) + game/auth URL constants |
| `IAuthService.cs` | `IAuthService` | Auth abstraction: guest/social login, refresh, logout, token persistence |
| `ITokenStorage.cs` | `ITokenStorage` | Token persistence abstraction; Dev=PlayerPrefs, Prod=AES-encrypted PlayerPrefs |
| `PlayerPrefsTokenStorage.cs` | `PlayerPrefsTokenStorage` | Dev token storage backed by PlayerPrefs |
| `SecureTokenStorage.cs` | `SecureTokenStorage` | Prod token storage: AES-encrypted PlayerPrefs using device ID as key |
| `PlatformAuthService.cs` | `PlatformAuthService` | Real platform auth HTTP adapter for guest/google/refresh/logout; uses ITokenStorage |
| `UiEventBus.cs` | `UiEventBus` | Typed event bus for UI busy/error/viewmodel/auth/balance/inventory events |
| `UserDataCache.cs` | `UserDataCache` | DontDestroyOnLoad singleton; caches soft balance + item inventory across scenes; publishes BalanceChanged/InventoryChanged on mutation; cleared on Title scene start |
| `ToastPresenter.cs` | `ToastPresenter` | Global top-stack toast renderer subscribed to `UiErrorRaised` |
| `NetworkManager.cs` | `NetworkManager` | HTTP GET/POST/PATCH coroutines with client/protocol/auth headers; delegates auth to IAuthService; clears token on 401 |
| `PoolManager.cs` | `PoolManager` | Keyed GameObject pool |
| `LocalizationManager.cs` | `LocalizationManager` | String/error table + language switching; fires LanguageChanged |
| `FontRegistry.cs` | `FontRegistry` | ScriptableObject: LanguageCode -> TMP_FontAsset pair |

## Symbols
| symbol | kind | note |
|---|---|---|
| `GameContext.SelectedStageId` | prop | set before `SceneLoader.LoadScene("Game")` |
| `GameContext.SetStageSession(string,int,int,Dictionary<int,int>)` | method | stores server stage session token, move limit, time limit, start timestamp, and item counts |
| `GameContext.ItemCounts` | prop | `Dictionary<int,int>` — item quantities from last `SetStageSession`; reset by `ClearStageSession` |
| `GameContext.ClearStageSession()` | method | clears server stage-session state including ItemCounts |
| `GameContext.IsStreakChallengeActive` | prop | true when a 24H streak challenge cycle is active for the current session |
| `GameContext.SuppressNextTitleSilentLogin()` | method | one-shot guard for intentional Lobby -> Title navigation |
| `GameContext.ConsumeTitleSilentLoginSuppression()` | method | Title entry consumes the one-shot silent-login guard |
| `GameStateMachine.TryTransition(GameState)` | method | returns false if transition invalid |
| `GameStateMachine.OnStateChanged` | event | `Action<GameState,GameState>` (from, to) |
| `InGameController.Instance` | prop | singleton; valid during Game scene lifetime |
| `InGameController.OpenPausePopup()` | method | opens PausePopup with timer pause/resume callbacks |
| `InGameController.AbandonStageAndLoad(string)` | method | submits stage fail with active session token, clears context, then loads target scene |
| `InGameController.ExtendTime(int)` | method | CloseAll popups + `_timer.Start(seconds)` (resets IsExpired) + SetInputEnabled(true); called by TimeoutPopup on successful extend API response |
| `InGameController.HandleStageStarted(...)` | method | enables gameplay only after server stage-start success; insufficient stamina opens Energy popup and returns Lobby; saves ItemCounts from response |
| `InGameController.OnItemButtonPressed(int)` | method | routes to item-specific handler: OBSTACLE_REMOVE→ActivateObstacleRemover, NODE_PAIR_REMOVE→ActivateNodePairEraser, MOVE_REDUCE→UseMoveReducer, TIME_EXTEND→UseTimeExtender |
| `InGameController.ActivateObstacleRemover()` | method | enters item-selection mode; highlights obstacle cells; tap removes obstacle and applies server response |
| `InGameController.ActivateNodePairEraser()` | method | enters item-selection mode; highlights all node cells; tap removes node pair and checks cleared |
| `InGameController.UseMoveReducer()` | method | instant use; calls server then decrements `_movesUsed` by 3; guarded by `_movesUsed >= 3` |
| `InGameController.UseTimeExtender()` | method | instant use; calls server then adds 20 s to timer via `_timer.Start(remaining + 20)` |
| `InGameController.CancelItemSelection()` | method | exits item-selection mode; restores drag event subscriptions; clears board highlights |
| `InGameController.RefreshItemButtons()` | method | calls `_hud.SetItemButtonState` for all 4 items with current counts and extra conditions |
| `UIManager.GetLayer(UILayer)` | method | returns canvas Transform for named layer |
| `PopupId` | enum | popup ids: ReturnTitle, ExitGame, Settings, BuyItem, Energy, StreakChallenge, Account, Reward, StageClear, SessionExpired, Pause, ForceUpdate, Maintenance, StageDetail, ClearNextStageConfirm, DailyReward, Timeout, StreakRewardConfirm, ShopItemConfirm, ShopItemResult |
| `PopupManager.Request(PopupId,object)` | method | static event-driven popup request entry point |
| `PopupManager.Open<T>()` | method | code-instantiates legacy popup T on Popup layer, pushes stack |
| `PopupManager.CloseTop()` | method | destroys top popup, re-shows previous |
| `PopupBase.BindOverlayClose()` | method | binds the first child Button named "Overlay" to CloseTop; call from each prefab popup Init() |
| `PopupBase.Awake()` | method | virtual; sets Panel child localScale=0 (scale-in prep); adds `ButtonPressEffect` to all non-Overlay child Buttons |
| `PopupBase.Start()` | method | adds `ButtonPressEffect` to fallback-built buttons created in Init(); starts `RunOpenAnim` coroutine |
| `ButtonPressEffect.OnPointerDown/Up/Exit` | methods | animate current localScale * 0.88 on press, restore captured Vector3 only after active press; uses `Time.unscaledDeltaTime` |
| `AppEnvironment` | enum | `Dev` / `Prod` |
| `AppConfig.DevGameServerUrl` | const | `http://localhost:20101` |
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
| `BalanceChanged` | struct | event payload published by UserDataCache.SetBalance; carries SoftBalance |
| `InventoryChanged` | struct | event payload published by UserDataCache.SetInventoryItem; carries ItemId + Quantity |
| `UserDataCache.Instance` | prop | singleton; valid after Bootstrap |
| `UserDataCache.SetBalance(long)` | method | updates cached SoftBalance and publishes BalanceChanged |
| `UserDataCache.SetInventoryItem(int,int)` | method | updates cached item quantity and publishes InventoryChanged |
| `UserDataCache.GetInventoryItem(int)` | method | returns cached quantity for itemId (0 if absent) |
| `UserDataCache.GetInventory()` | method | returns IReadOnlyDictionary<int,int> snapshot |
| `UserDataCache.Clear()` | method | resets balance to 0 and clears inventory dict; called on Title scene entry |
| `ToastPresenter.OnError(UiErrorRaised)` | method | ignores blocking errors and renders localized non-critical errors as top toasts |
| `NetworkManager.AuthService` | prop | active IAuthService; defaults to `PlatformAuthService` |
| `NetworkManager.LoginGuest(Action<bool,string>)` | method | delegates explicit guest login to IAuthService |
| `NetworkManager.LoginGoogle(string,string,Action<bool,string>)` | method | delegates Google token exchange to IAuthService |
| `NetworkManager.RefreshAuth(Action<bool,string>)` | method | delegates refresh to IAuthService |
| `NetworkManager.EnsureGuestAuth(Action<bool,string>)` | method | delegates to IAuthService.EnsureAuth |
| `SceneLoader.HoldForReady()` | method | prevents FadeIn until NotifyReady() called; call from Awake of scenes that need data before reveal |
| `SceneLoader.NotifyReady()` | method | releases overlay hold set by HoldForReady |
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
- FSM valid transitions: Idle→Drawing, Drawing→Idle, Drawing→Completed, Idle→Completed (node-pair eraser may clear from Idle state).
- Pause popup must call `_timer.Pause()` / `_timer.Resume()` around show/dismiss.
