# OutGame/UI - Title, Lobby, navigation helpers, outgame popups

## Files
| file | class | role |
|---|---|---|
| `LocalizedText.cs` | `LocalizedText` | MonoBehaviour; auto-refreshes TMP text on LanguageChanged |
| `LanguageSelector.cs` | `LanguageSelector` | TMP_Dropdown wired to LocalizationManager |
| `RuntimeNavigationButtons.cs` | `RuntimeNavigationButtons` | Scene navigation + popup trigger entry points |
| `BootstrapWireframeController.cs` | `BootstrapWireframeController` | Bootstrap generated UI refs; renders server config/loading/version state |
| `TitleWireframeController.cs` | `TitleWireframeController` | Title generated UI refs; renders account/version state |
| `LobbyTabController.cs` | `LobbyTabController` | Lobby Shop/Home/Ranking tab switcher with Inspector-assignable refs |
| `LobbyWireframeController.cs` | `LobbyWireframeController` | Binds generated lobby wireframe refs to server DTOs and static catalog data |
| `SceneEscapeHandler.cs` | `SceneEscapeHandler` | Escape key EscapeAction (None/ReturnToTitle/ExitGame) |
| `SafeAreaFitter.cs` | `SafeAreaFitter` | Adjusts RectTransform anchors to device safe area in Awake |
| `ModernUI.cs` | `ModernUI` | Shared code-created glossy UI helper methods/colors |
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
| `SessionExpiredPopup.cs` | `SessionExpiredPopup` | Code-only popup (no prefab); shown when auth returns SESSION_EXPIRED; confirms → return to Title |

## Symbols
| symbol | kind | note |
|---|---|---|
| `LocalizedText.SetStringId(string)` | method | changes key + immediate refresh via LocalizationManager.Get |
| `RuntimeNavigationButtons.LoadGame()` | method | uses `defaultStageId` field; sets GameContext, loads "Game" |
| `RuntimeNavigationButtons.LoadGameWithStage(int)` | method | explicit stageId variant |
| `RuntimeNavigationButtons.OpenExitGamePopup()` | method | requests `PopupId.ExitGame` via PopupManager event |
| `RuntimeNavigationButtons.OpenSettingsPopup()` | method | requests `PopupId.Settings` via PopupManager event |
| `RuntimeNavigationButtons.OpenBuyItemPopup()` | method | requests `PopupId.BuyItem` via PopupManager event |
| `RuntimeNavigationButtons.OpenEnergyPopup()` | method | requests `PopupId.Energy` via PopupManager event |
| `RuntimeNavigationButtons.OpenAccountPopup()` | method | requests `PopupId.Account` via PopupManager event |
| `RuntimeNavigationButtons.OpenDailyChallengePopup()` | method | requests `PopupId.DailyChallenge` via PopupManager event |
| `RuntimeNavigationButtons.OpenRewardPopup()` | method | requests `PopupId.Reward` via PopupManager event |
| `BootstrapWireframeController.Start()` | method | fetches bootstrap config through `IUiDataService` and updates serialized refs |
| `TitleWireframeController.Start()` | method | fetches bootstrap/account state through `IUiDataService` and updates serialized refs |
| `LobbyTabController.Configure(...)` | method | assigns tab buttons and tab panels from generated UI builder |
| `LobbyTabController.shopTabButton` | field | `[SerializeField]` Shop tab button, Inspector-assignable |
| `LobbyTabController.homeTabButton` | field | `[SerializeField]` Home tab button, Inspector-assignable |
| `LobbyTabController.rankingTabButton` | field | `[SerializeField]` Ranking tab button, Inspector-assignable |
| `LobbyTabController.shopPanel` | field | `[SerializeField]` Shop tab panel; includes ScrollView |
| `LobbyTabController.homePanel` | field | `[SerializeField]` Home tab panel |
| `LobbyTabController.rankingPanel` | field | `[SerializeField]` Ranking tab panel; includes ScrollView |
| `LobbyWireframeController.RefreshRanking(string)` | method | fetches ranking segment from `IUiDataService` and renders rows from DTO entries |
| `ModernUI.AddPanel(...)` | method | creates themed Image panel with optional shadow |
| `ModernUI.AddLocalizedLabel(...)` | method | creates TMP label bound to `LocalizedText` |
| `ModernUI.AddLocalizedButton(...)` | method | creates themed Button with localized TMP label |
| `ConfirmPopupBase.Build(string,string,string,Color,Action)` | method | legacy title/message/confirmLabel/accent/onConfirm builder |
| `ConfirmPopupBase.AddLocalizedLabel(...)` | method | legacy protected helper; adds TMP label with LocalizedText component |
| `ConfirmPopupBase.AddLocalizedButton(...)` | method | legacy protected helper; adds button with localized label |
| `LobbyStageMapView.Build()` | method | instantiates/pools all stage node buttons |
| `LobbyStageMapView.NextPage()` / `PreviousPage()` | methods | pagination control |
| `SceneEscapeHandler.action` | field | `[SerializeField]` EscapeAction enum |
| `ReturnTitlePopup.Init()` | method | binds Inspector refs or fallback buttons: CloseIconButton, CancelButton, ConfirmButton |
| `ExitGamePopup.Init(RuntimeNavigationButtons)` | method | binds Inspector refs or fallback buttons: CloseIconButton, CancelButton, ConfirmButton |
| `SettingPopup.Init()` | method | binds Inspector refs or fallback buttons: CloseIconButton, CloseButton, SaveButton |
| `BuyItemPopup.Init()` | method | binds Inspector refs or fallback buttons: CloseIconButton, CloseButton, BuyButton |
| `EnergyPopup.Init()` | method | binds Inspector refs or fallback buttons: CloseIconButton, CloseButton, WatchAdButton, RefillButton |
| `DailyChallengePopup.Init()` | method | fetches `DailyChallengeResponse` and renders countdown, streak tiles, reward preview |
| `AccountPopup.Init()` | method | fetches `AccountMeResponse` and renders profile/link state |
| `RewardPopup.Init(string,string)` | method | prepares reward claim buttons for base and ad-multiplied claims |
| `SessionExpiredPopup.Init()` | method | builds code-only UI via `ConfirmPopupBase.Build()`; confirm → return to Title |

## Cross-refs
- Consumed by: client `Core.PopupManager` (outgame popups loaded from `Resources/Prefabs/UI/` and pushed via PopupManager.Request)
- Depends on: client `Core.GameContext` (stage selection state), `shared/datas/string/` (LocalizedText for all visible labels)

## Rules
- Namespace: `ProjectLink.OutGame.UI`
- Scene navigation uses `SceneLoader.LoadScene`; shared state uses `GameContext`
- Repeated runtime UI (stage nodes) uses `PoolManager`
- `SafeAreaFitter` attach to root canvas panel; handles notch/home-bar insets automatically
- New popup prefabs are generated as visible wireframe image slots plus Button hotspots under `Assets/Resources/Prefabs/UI`.
- Popup and lobby controllers expose serialized refs for Inspector assignment and also fallback-find by child name.
- Visible runtime-created strings must use `LocalizedText` and client string IDs unless they are numeric state or icon glyphs.
