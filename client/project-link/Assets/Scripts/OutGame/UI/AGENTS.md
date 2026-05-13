# OutGame/UI - Title, Lobby, navigation helpers, outgame popups

## Files
| file | class | role |
|---|---|---|
| `LocalizedText.cs` | `LocalizedText` | MonoBehaviour; auto-refreshes TMP text on LanguageChanged |
| `LanguageSelector.cs` | `LanguageSelector` | TMP_Dropdown wired to LocalizationManager; builds dropdown template programmatically in Awake if not assigned |
| `RuntimeNavigationButtons.cs` | `RuntimeNavigationButtons` | Scene navigation + popup trigger entry points |
| `BootstrapWireframeController.cs` | `BootstrapWireframeController` | Bootstrap generated UI refs; renders `BootstrapViewModel` loading/version/force-update state |
| `TitleWireframeController.cs` | `TitleWireframeController` | Title generated UI refs; renders `TitleViewModel` auth/version/maintenance state |
| `LobbyTabController.cs` | `LobbyTabController` | Lobby Shop/Home/Ranking tab switcher with Inspector-assignable refs and selected-tab visual state |
| `LobbyWireframeController.cs` | `LobbyWireframeController` | Binds generated lobby wireframe refs to `LobbyViewModel` state and server progress |
| `SceneEscapeHandler.cs` | `SceneEscapeHandler` | Escape key EscapeAction (None/ReturnToTitle/ExitGame/OpenPauseMenu) |
| `SafeAreaFitter.cs` | `SafeAreaFitter` | Adjusts RectTransform anchors to device safe area in Awake |
| `ModernUI.cs` | `ModernUI` | Shared code-created glossy UI helper methods/colors |
| `RepeatButton.cs` | `RepeatButton` | Pointer hold helper that repeats button actions for carousel navigation |
| `LobbyStageMapView.cs` | `LobbyStageMapView` | Paginated stage button grid |
| `ConfirmPopupBase.cs` | `ConfirmPopupBase` | Abstract base; provides panel/button/label builder helpers |
| `ExitGamePopup.cs` | `ExitGamePopup` | Prefab controller; binds cancel/confirm hotspots for quit |
| `ReturnTitlePopup.cs` | `ReturnTitlePopup` | Prefab controller; binds cancel/confirm hotspots for title return |
| `SettingPopup.cs` | `SettingPopup` | Prefab controller; binds close/save hotspots |
| `BuyItemPopup.cs` | `BuyItemPopup` | Prefab controller; binds close/buy hotspots |
| `EnergyPopup.cs` | `EnergyPopup` | Prefab controller; binds close/watch/refill hotspots |
| `DailyChallengePopup.cs` | `DailyChallengePopup` | Prefab controller; binds daily challenge server state to streak/reward/play refs |
| `AccountPopup.cs` | `AccountPopup` | Prefab controller; binds account/profile server state |
| `RewardPopup.cs` | `RewardPopup` | Prefab controller; claims reward through `IUiDataService` |
| `SessionExpiredPopup.cs` | `SessionExpiredPopup` | Code-only popup; auth expiry confirm -> Title |
| `ForceUpdatePopup.cs` | `ForceUpdatePopup` | Prefab popup; non-dismissible; single store CTA |
| `MaintenancePopup.cs` | `MaintenancePopup` | Prefab popup; non-dismissible; displays server maintenance message in Txt_Body |
| `StageDetailPopup.cs` | `StageDetailPopup` | Prefab popup; dismissible; shows stage stars/best score/rank; Btn_Play -> Game scene |

## Symbols
| symbol | kind | note |
|---|---|---|
| `LocalizedText.SetStringId(string)` | method | changes key + immediate refresh via LocalizationManager.Get |
| `RuntimeNavigationButtons.LoadGame()` | method | uses `defaultStageId` field; sets GameContext, loads Game |
| `RuntimeNavigationButtons.OpenStageDetail(int)` | method | opens StageDetail popup for lobby stage selection |
| `RuntimeNavigationButtons.EnterStage(int)` | method | server stage-start guard; returns Lobby and opens Energy popup on insufficient stamina |
| `RuntimeNavigationButtons.OpenSettingsPopup()` | method | requests `PopupId.Settings` |
| `RuntimeNavigationButtons.OpenEnergyPopup()` | method | requests `PopupId.Energy` |
| `RuntimeNavigationButtons.OpenAccountPopup()` | method | requests `PopupId.Account` |
| `RuntimeNavigationButtons.OpenDailyChallengePopup()` | method | requests `PopupId.DailyChallenge` |
| `RuntimeNavigationButtons.OpenPausePopup()` | method | delegates to InGameController in Game scene or requests `PopupId.Pause` |
| `BootstrapWireframeController.Start()` | method | creates `BootstrapViewModel`, binds retry button, starts bootstrap load |
| `BootstrapWireframeController.Render()` | method | renders loading/error/version and opens ForceUpdate or Title scene |
| `TitleWireframeController.Start()` | method | creates `TitleViewModel`, owns auth button listeners |
| `TitleWireframeController.Render()` | method | renders auth loading state and opens ForceUpdate/Maintenance/Lobby after scene transition is idle |
| `LobbyTabController.Configure(...)` | method | assigns tab buttons and tab panels from generated UI builder |
| `LobbyWireframeController.RefreshRanking(string)` | method | clears current ranking rows and requests selected ranking segment through `LobbyViewModel` |
| `LobbyWireframeController.Render()` | method | renders Lobby/Shop/Ranking viewmodel state and localized errors; stage carousel initial selection comes from Lobby API, bounds from CSV catalog, play/stars from server progress |
| `RepeatButton.Repeated` | event | fires after long-press delay at repeat interval while button remains pressed |
| `ConfirmPopupBase.Build(...)` | method | legacy title/message/confirmLabel/accent/onConfirm builder |
| `LobbyStageMapView.Build()` | method | instantiates/pools all stage node buttons |
| `SceneEscapeHandler.action` | field | `[SerializeField]` EscapeAction enum |
| `ReturnTitlePopup.Init()` | method | binds close/cancel/confirm hotspots |
| `ExitGamePopup.Init(RuntimeNavigationButtons)` | method | binds close/cancel/confirm hotspots |
| `SettingPopup.Init()` | method | binds close/save hotspots; adds LanguageSelector to TMP_Dropdown child at runtime if missing |
| `BuyItemPopup.Init()` | method | binds close/buy hotspots |
| `EnergyPopup.Init()` | method | binds close/watch/refill hotspots |
| `DailyChallengePopup.Init()` | method | fetches DailyChallengeResponse and renders streak/reward/play refs |
| `AccountPopup.Init()` | method | fetches AccountMeResponse and renders profile/link state |
| `RewardPopup.Init(string,string)` | method | prepares reward claim buttons |
| `SessionExpiredPopup.Init()` | method | confirm -> Title |
| `ForceUpdatePopup.Init()` | method | binds Btn_OpenStore click -> platform app store URL |
| `MaintenancePopup.Init(string)` | method | sets Txt_Body text from server maintenance message |
| `StageDetailPopup.Init(int)` | method | binds Btn_Close/Btn_Play, renders stars and top-10 stage ranking (score, descending); adds MyRankPanel below scroll showing `#{rank} {score}` or `-` |

## Cross-refs
- Consumed by: client `Core.PopupManager`
- Depends on: client `Core.GameContext`, client `Core.UiEventBus`, client `Services.UiScreenViewModels`, `shared/datas/string/`

## Rules
- Namespace: `ProjectLink.OutGame.UI`.
- Scene navigation uses `SceneLoader.LoadScene`; shared state uses `GameContext`.
- Popup and lobby controllers expose serialized refs for Inspector assignment and fallback-find by child name.
- Visible runtime-created strings must use `LocalizedText` and client string IDs unless they are numeric state or icon glyphs.
