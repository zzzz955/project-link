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
| `StreakChallengePopup.cs` | `StreakChallengePopup` | Code popup; fetches streak state, builds vertical level list, handles activate/startLevel/claimReward |
| `StreakChallengeBadge.cs` | `StreakChallengeBadge` | MonoBehaviour badge in Carousel_Stages top-left; state-driven color, 5s bounce animation, opens StreakChallenge popup on tap |
| `AccountPopup.cs` | `AccountPopup` | Prefab controller; binds account/profile server state |
| `RewardPopup.cs` | `RewardPopup` | Prefab controller; claims reward through `IUiDataService` |
| `SessionExpiredPopup.cs` | `SessionExpiredPopup` | Code-only popup; auth expiry confirm -> Title |
| `ForceUpdatePopup.cs` | `ForceUpdatePopup` | Prefab popup; non-dismissible; single store CTA |
| `MaintenancePopup.cs` | `MaintenancePopup` | Prefab popup; non-dismissible; displays server maintenance message in Txt_Body |
| `StageDetailPopup.cs` | `StageDetailPopup` | Prefab popup; dismissible; shows stage title (popup.stage.title_n_fmt) + stars + top-10 ranking; Btn_Play -> Game scene; no Txt_Best/Txt_MyRank |
| `StreakRewardConfirmPopup.cs` | `StreakRewardConfirmPopup`, `StreakRewardConfirmModel` | Code popup; confirms whether to go Lobby (claim streak reward) or continue to next stage after OPEN_REWARD_POPUP directive |

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
| `RuntimeNavigationButtons.OpenStreakChallengePopup()` | method | requests `PopupId.StreakChallenge` |
| `RuntimeNavigationButtons.OpenPausePopup()` | method | delegates to InGameController in Game scene or requests `PopupId.Pause` |
| `BootstrapWireframeController.Start()` | method | creates `BootstrapViewModel`, binds retry button, starts bootstrap load |
| `BootstrapWireframeController.Render()` | method | renders loading/error/version and opens ForceUpdate or Title scene |
| `TitleWireframeController.Start()` | method | creates `TitleViewModel`, owns auth button listeners |
| `TitleWireframeController.Render()` | method | renders auth loading state and opens ForceUpdate/Maintenance/Lobby after scene transition is idle |
| `LobbyTabController.Configure(...)` | method | assigns tab buttons and tab panels from generated UI builder |
| `LobbyWireframeController.RefreshRanking(string)` | method | clears current ranking rows and requests selected ranking segment through `LobbyViewModel` |
| `LobbyWireframeController.Render()` | method | renders Lobby/Shop/Ranking viewmodel state and localized errors; stage carousel initial selection comes from Lobby API, bounds from CSV catalog, play/stars from server progress |
| `LobbyWireframeController.RefreshStaminaTimer()` | method | shows "Full" (status.stamina_full) when `_staminaFull`; else "MM:SS" countdown to next recharge |
| `LobbyWireframeController.RenderCenterStarImages(int)` | method | updates Img_Star_0/1/2 in Group_Stars under StageNode_Center using starOnSprite/starOffSprite; fallback to yellow/dim color when sprites null |
| `LobbyWireframeController._staminaFull` | field | bool; set in ApplyLobby when StaminaCurrent >= StaminaMax; drives RefreshStaminaTimer display |
| `LobbyWireframeController.AnimateCount(label,target,duration,formatter)` | coroutine | SmoothStep count-up from 0 → target over duration (unscaled time); used for stamina/coin on first lobby load |
| `RepeatButton.Repeated` | event | fires after long-press delay at repeat interval while button remains pressed |
| `ConfirmPopupBase.Build(...)` | method | legacy title/message/confirmLabel/accent/onConfirm builder |
| `LobbyStageMapView.Build()` | method | instantiates/pools all stage node buttons |
| `SceneEscapeHandler.action` | field | `[SerializeField]` EscapeAction enum |
| `ReturnTitlePopup.Init()` | method | binds close/cancel/confirm hotspots |
| `ExitGamePopup.Init(RuntimeNavigationButtons)` | method | binds close/cancel/confirm hotspots |
| `SettingPopup.Init()` | method | binds close/save hotspots; loads player settings; adds LanguageSelector to TMP_Dropdown child at runtime if missing; starts coroutine-based toggle animations on change |
| `SettingPopup.AnimateToggleVisual(toggle,isOn)` | method | stops any running animation for the toggle, starts `ToggleAnim` coroutine |
| `SettingPopup.ToggleAnim(toggle,isOn)` | coroutine | scale-compress (0.82) → swap Img_Off/Img_On alpha → bounce-overshoot (1.10→1.0) over 0.25 s |
| `BuyItemPopup.Init()` | method | binds close/buy hotspots |
| `EnergyPopup.Init()` | method | binds close/watch/refill hotspots |
| `StreakChallengePopup.Init()` | method | fetches `StreakChallengeStateResponse` and renders level list; idempotent |
| `StreakChallengeBadge.Apply(string?,bool,string)` | method | updates badge color/label/progress text and animation flag based on event state |
| `AccountPopup.Init()` | method | fetches AccountMeResponse and renders profile/link state |
| `RewardPopup.Init(string,string)` | method | prepares reward claim buttons |
| `SessionExpiredPopup.Init()` | method | confirm -> Title |
| `ForceUpdatePopup.Init()` | method | binds Btn_OpenStore click -> platform app store URL |
| `MaintenancePopup.Init(string)` | method | sets Txt_Body text from server maintenance message |
| `StageDetailPopup.Init(int)` | method | binds Btn_Close/Btn_Play; sets dynamic title via `popup.stage.title_n_fmt`; renders stars and top-10 stage ranking (score, descending); no MyRankPanel, no Txt_Best/Txt_MyRank |
| `StreakRewardConfirmPopup.Init(StreakRewardConfirmModel)` | method | Btn_Lobby: sets `ShouldOpenStreakPopupOnLobby=true` + CloseAll + LoadScene("Lobby"); Btn_Continue: CloseAll + EnterStage if unlocked, else Lobby |

## Cross-refs
- Consumed by: client `Core.PopupManager`
- Depends on: client `Core.GameContext`, client `Core.UiEventBus`, client `Services.UiScreenViewModels`, `shared/datas/string/`

## Rules
- Namespace: `ProjectLink.OutGame.UI`.
- Scene navigation uses `SceneLoader.LoadScene`; shared state uses `GameContext`.
- Popup and lobby controllers expose serialized refs for Inspector assignment and fallback-find by child name.
- Visible runtime-created strings must use `LocalizedText` or `LocalizationManager.Get(key)` with client string IDs; never hardcode user-facing Korean/EN text. Dynamic labels (e.g. streak button) use `LocalizationManager.Get` directly; static labels use `LocalizedText` component.
- StreakChallengePopup dynamic button labels: `streak.activate`, `streak.start_level` (format arg: level 1-based int), `streak.claim`.
- Stamina timer text: `status.stamina_full` when full; else "MM:SS" countdown. Key added to clientstring.csv.
- StageDetailPopup title: dynamic via `popup.stage.title_n_fmt` (format arg: stageId int). LocalizedText component disabled on Txt_Title before setting text.
- "Guest" display name: use `LocalizationManager.Get("popup.account.guest")` (AccountPopup, SettingPopup, StageDetailPopup ranking display name fallback).
- Toggle visuals: `Img_Off` (alpha 1 = off) and `Img_On` (alpha 1 = on) children of the Toggle transform; no `Handle` child.
- LobbyTabController.SetTabVisual: Indicator find is null-safe; Indicator no longer exists in generated TabBar.
