# OutGame/UI — Title, Lobby, navigation helpers, outgame popups

## Files
| file | class | role |
|---|---|---|
| `LocalizedText.cs` | `LocalizedText` | MonoBehaviour; auto-refreshes TMP text on LanguageChanged |
| `LanguageSelector.cs` | `LanguageSelector` | TMP_Dropdown wired to LocalizationManager |
| `RuntimeNavigationButtons.cs` | `RuntimeNavigationButtons` | Scene navigation + popup trigger entry points |
| `SceneEscapeHandler.cs` | `SceneEscapeHandler` | Escape key → EscapeAction (None/ReturnToTitle/ExitGame) |
| `SafeAreaFitter.cs` | `SafeAreaFitter` | Adjusts RectTransform anchors to device safe area in Awake |
| `LobbyStageMapView.cs` | `LobbyStageMapView` | Paginated stage button grid |
| `ConfirmPopupBase.cs` | `ConfirmPopupBase` | Abstract base; provides panel/button/label builder helpers |
| `ExitGamePopup.cs` | `ExitGamePopup` | Exit-application confirmation (extends ConfirmPopupBase) |
| `ReturnTitlePopup.cs` | `ReturnTitlePopup` | Return-to-title confirmation (extends ConfirmPopupBase) |

## Symbols
| symbol | kind | note |
|---|---|---|
| `LocalizedText.SetStringId(string)` | method | changes key + immediate refresh via LocalizationManager.Get |
| `RuntimeNavigationButtons.LoadGame()` | method | uses `defaultStageId` field; sets GameContext, loads "Game" |
| `RuntimeNavigationButtons.LoadGameWithStage(int)` | method | explicit stageId variant |
| `RuntimeNavigationButtons.OpenExitGamePopup()` | method | opens ExitGamePopup via PopupManager |
| `ConfirmPopupBase.Build(string,string,string,Color,Action)` | method | title/message/confirmLabel/accent/onConfirm |
| `ConfirmPopupBase.AddLocalizedLabel(...)` | method | protected; adds TMP label with LocalizedText component |
| `ConfirmPopupBase.AddLocalizedButton(...)` | method | protected; adds button with localized label |
| `LobbyStageMapView.Build()` | method | instantiates/pools all stage node buttons |
| `LobbyStageMapView.NextPage()` / `PreviousPage()` | methods | pagination control |
| `SceneEscapeHandler.action` | field | `[SerializeField]` EscapeAction enum |

## Rules
- Namespace: `ProjectLink.OutGame.UI`
- Scene navigation → `SceneLoader.LoadScene`; shared state → `GameContext`
- Repeated runtime UI (stage nodes) → `PoolManager`
- `SafeAreaFitter` attach to root canvas panel; handles notch/home-bar insets automatically
