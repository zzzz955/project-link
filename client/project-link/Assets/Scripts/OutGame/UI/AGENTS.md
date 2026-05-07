# OutGame/UI - Title, Lobby, navigation helpers, outgame popups

## Files
| file | class | role |
|---|---|---|
| `LocalizedText.cs` | `LocalizedText` | MonoBehaviour; auto-refreshes TMP text on LanguageChanged |
| `LanguageSelector.cs` | `LanguageSelector` | TMP_Dropdown wired to LocalizationManager |
| `RuntimeNavigationButtons.cs` | `RuntimeNavigationButtons` | Scene navigation + popup trigger entry points |
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
| `ModernUI.AddPanel(...)` | method | creates themed Image panel with optional shadow |
| `ModernUI.AddLocalizedLabel(...)` | method | creates TMP label bound to `LocalizedText` |
| `ModernUI.AddLocalizedButton(...)` | method | creates themed Button with localized TMP label |
| `ConfirmPopupBase.Build(string,string,string,Color,Action)` | method | legacy title/message/confirmLabel/accent/onConfirm builder |
| `ConfirmPopupBase.AddLocalizedLabel(...)` | method | legacy protected helper; adds TMP label with LocalizedText component |
| `ConfirmPopupBase.AddLocalizedButton(...)` | method | legacy protected helper; adds button with localized label |
| `LobbyStageMapView.Build()` | method | instantiates/pools all stage node buttons |
| `LobbyStageMapView.NextPage()` / `PreviousPage()` | methods | pagination control |
| `SceneEscapeHandler.action` | field | `[SerializeField]` EscapeAction enum |
| `ReturnTitlePopup.Init()` | method | binds prefab buttons: CloseIconButton, CancelButton, ConfirmButton |
| `ExitGamePopup.Init(RuntimeNavigationButtons)` | method | binds prefab buttons: CloseIconButton, CancelButton, ConfirmButton |
| `SettingPopup.Init()` | method | binds prefab buttons: CloseIconButton, CloseButton, SaveButton |
| `BuyItemPopup.Init()` | method | binds prefab buttons: CloseIconButton, CloseButton, BuyButton |
| `EnergyPopup.Init()` | method | binds prefab buttons: CloseIconButton, CloseButton, WatchAdButton, RefillButton |

## Rules
- Namespace: `ProjectLink.OutGame.UI`
- Scene navigation uses `SceneLoader.LoadScene`; shared state uses `GameContext`
- Repeated runtime UI (stage nodes) uses `PoolManager`
- `SafeAreaFitter` attach to root canvas panel; handles notch/home-bar insets automatically
- New popup prefabs are generated as wireframe image slots plus transparent hotspots under `Assets/Resources/Prefabs/UI`.
- Visible runtime-created strings must use `LocalizedText` and client string IDs unless they are numeric state or icon glyphs.
