# OutGame/UI - Title, Lobby, navigation helpers, outgame popups

## Files
| file | class | role |
|---|---|---|
| `LocalizedText.cs` | `LocalizedText` | MonoBehaviour; auto-refreshes TMP text + font on LanguageChanged (static labels with stringId) |
| `LocalizedFont.cs` | `LocalizedFont` | MonoBehaviour; auto-applies FontRegistry font on Awake/OnEnable/LanguageChanged (dynamic labels without stringId); UIBuilder adds this to all TMP labels that lack LocalizedText |
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
| `UIIconAnimator.cs` | `UIIconAnimator` | MonoBehaviour; every 5 s plays bounce (scale punch) + additive glow (child Img_Glow, UIGlow shader) on icon Images; added by UIBuilder to Icon_*/btn_icon_* GOs |
| `LobbyStageMapView.cs` | `LobbyStageMapView` | Paginated stage button grid |
| `ConfirmPopupBase.cs` | `ConfirmPopupBase` | Abstract base; provides panel/button/label builder helpers |
| `ExitGamePopup.cs` | `ExitGamePopup` | Prefab controller; binds cancel/confirm hotspots for quit |
| `ReturnTitlePopup.cs` | `ReturnTitlePopup` | Prefab controller; binds cancel/confirm hotspots for title return |
| `SettingPopup.cs` | `SettingPopup` | Prefab controller; binds close/save hotspots |
| `BuyItemPopup.cs` | `BuyItemPopup` | Prefab controller; binds close/buy hotspots |
| `EnergyPopup.cs` | `EnergyPopup` | Prefab controller; binds close/watch/refill hotspots |
| `StreakChallengePopup.cs` | `StreakChallengePopup` | Popup controller; renders banner/timer/level/prize/path from streak state + CSV catalog, handles info/close/activate/startLevel/claimReward |
| `StreakChallengeBadge.cs` | `StreakChallengeBadge` | MonoBehaviour badge in Carousel_Stages top-left; state-driven color, 5s bounce animation, opens StreakChallenge popup on tap |
| `AccountPopup.cs` | `AccountPopup` | Prefab controller; binds account/profile server state |
| `RewardPopup.cs` | `RewardPopup` | Prefab controller; claims reward through `IUiDataService` |
| `SessionExpiredPopup.cs` | `SessionExpiredPopup` | Code-only popup; auth expiry confirm -> Title |
| `ForceUpdatePopup.cs` | `ForceUpdatePopup` | Prefab popup; non-dismissible; single store CTA |
| `MaintenancePopup.cs` | `MaintenancePopup` | Prefab popup; non-dismissible; displays server maintenance message in Txt_Body |
| `StageDetailPopup.cs` | `StageDetailPopup` | Prefab popup; dismissible; shows stage title (popup.stage.title_n_fmt) + stars + top-10 ranking; Btn_Play -> Game scene; no Txt_Best/Txt_MyRank |
| `StreakRewardConfirmPopup.cs` | `StreakRewardConfirmPopup`, `StreakRewardConfirmModel` | Code popup; confirms whether to go Lobby (claim streak reward) or continue to next stage after OPEN_REWARD_POPUP directive |
| `ShopItemConfirmPopup.cs` | `ShopItemConfirmPopup`, `ShopItemConfirmModel` | Prefab popup; shows item name, current balance, cost, post-purchase balance; Btn_Buy calls PurchaseItem API |
| `ShopItemResultPopup.cs` | `ShopItemResultPopup`, `ShopItemResultModel` | Prefab popup; shows dynamic title (success/fail), error message on fail, Btn_Confirm |
| `ShopProductCard.cs` | `ShopProductCard` | Prefab MonoBehaviour; renders one ingame item card (icon + title + price); tap opens ShopItemConfirm popup |
| `RankingCard.cs` | `RankingCard` | Prefab MonoBehaviour; renders ranking row/pinned-my-rank card with rank/medal, display name, avatar, and level/value |
| `ShopInventoryStrip.cs` | `ShopInventoryStrip` | MonoBehaviour; calls GetInventory, renders 4 item cells (icon + count) above shop viewport; dims cell at alpha 0.4 when count=0 |

## Symbols
| symbol | kind | note |
|---|---|---|
| `LocalizedText.SetStringId(string)` | method | changes key + immediate refresh via LocalizationManager.Get |
| `LocalizedFont.Apply()` | method | resolves FontRegistry font for current language + bold style; assigns to TMP label + ForceMeshUpdate(false,true); no-op if FontRegistry/LocalizationManager not ready |
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
| `LobbyWireframeController.AddRankingCard(parent,entry,pinned)` | method | instantiates `RankingCard` prefab for list rows and pinned my-rank; rank >=1000 displays `1000+` |
| `LobbyWireframeController.RefreshStaminaTimer()` | method | shows "Full" (status.stamina_full) when `_staminaFull`; else "MM:SS" countdown; font applied separately via `ApplyFontsToAllLabels` |
| `LobbyWireframeController.RenderCenterStarImages(int)` | method | updates Img_Star_0/1/2 in Group_Stars under StageNode_Center using starOnSprite/starOffSprite; fallback to yellow/dim color when sprites null |
| `LobbyWireframeController.BindAvatarButton()` | method | adds runtime Account popup listener only when Slot_Avatar has no persistent OpenAccountPopup handler |
| `LobbyWireframeController._staminaFull` | field | bool; set in ApplyLobby when StaminaCurrent >= StaminaMax; drives RefreshStaminaTimer display |
| `LobbyWireframeController.AnimateCount(label,target,duration,formatter)` | coroutine | SmoothStep count-up from 0 → target over duration (unscaled time); used for stamina/coin on first lobby load |
| `RepeatButton.Repeated` | event | fires after long-press delay at repeat interval while button remains pressed |
| `ConfirmPopupBase.Build(...)` | method | legacy title/message/confirmLabel/accent/onConfirm builder |
| `LobbyStageMapView.Build()` | method | instantiates/pools all stage node buttons |
| `SceneEscapeHandler.action` | field | `[SerializeField]` EscapeAction enum |
| `ReturnTitlePopup.Init()` | method | binds close/cancel/confirm hotspots |
| `ExitGamePopup.Init(RuntimeNavigationButtons)` | method | binds close/cancel/confirm hotspots |
| `SettingPopup.Init()` | method | binds close/save hotspots; reads BGM/SFX/haptics/notifications from `DataManager` (PlayerPrefs); adds LanguageSelector to TMP_Dropdown child at runtime if missing; starts coroutine-based toggle animations on change; no API calls |
| `SettingPopup.AnimateToggleVisual(toggle,isOn)` | method | stops any running animation for the toggle, starts `ToggleAnim` coroutine |
| `SettingPopup.ToggleAnim(toggle,isOn)` | coroutine | scale-compress (0.82, 0.07 s) → swap `Img_Toggle` sprite (slot_toggle_on/off) at min scale → bounce-overshoot (1.10→1.0, 0.18 s) |
| `BuyItemPopup.Init()` | method | binds close/buy hotspots |
| `EnergyPopup.Init()` | method | binds close/watch/refill hotspots |
| `StreakChallengePopup.Init()` | method | binds close/info/action buttons, fetches `StreakChallengeStateResponse`, renders current level path/prize/timer; idempotent |
| `StreakChallengePopup.RenderPrize(StreakChallengeStateResponse,int)` | method | resolves current level reward from `StaticCatalogService.GetStreakChallengeRewardItems` and displays soft currency/item reward |
| `StreakChallengePopup.DrawPath(RectTransform,int,int)` | method | renders alternating left/right level clear platforms that converge toward center from `requiredClearCount` |
| `StreakChallengeBadge.Apply(string?,bool,string)` | method | updates badge color/label/progress text and animation flag based on event state |
| `AccountPopup.Init()` | method | fetches AccountMeResponse and renders profile/link state |
| `RewardPopup.Init(string,string)` | method | prepares reward claim buttons |
| `SessionExpiredPopup.Init()` | method | confirm -> Title |
| `ForceUpdatePopup.Init()` | method | binds Btn_OpenStore click -> platform app store URL |
| `MaintenancePopup.Init(string)` | method | sets Txt_Body text from server maintenance message |
| `StageDetailPopup.Init(int)` | method | binds Btn_Close/Btn_Play; sets dynamic title via `popup.stage.title_n_fmt`; renders stars and top-10 stage ranking (score, descending); no MyRankPanel, no Txt_Best/Txt_MyRank |
| `StreakRewardConfirmPopup.Init(StreakRewardConfirmModel)` | method | Btn_Lobby: sets `ShouldOpenStreakPopupOnLobby=true` + CloseAll + LoadScene("Lobby"); Btn_Continue: CloseAll + EnterStage if unlocked, else Lobby |
| `ShopItemConfirmModel.ProductId` | field | int; shop product id for PurchaseShopProduct call |
| `ShopItemConfirmModel.Cost` | field | int; soft currency cost |
| `ShopItemConfirmModel.CurrentBalance` | field | long; balance at time of card tap |
| `ShopItemConfirmModel.DescriptionKey` | field | string; clientstring key for item description; empty string when not set |
| `ShopItemConfirmPopup.Init(ShopItemConfirmModel)` | method | renders Txt_Description (localized via DescriptionKey), balance/cost/after rows; Btn_Buy calls PurchaseShopProduct → updates UserDataCache → ShopItemResult |
| `ShopProductCard.Init(productId,itemName,cost,getBalance,icon,onPurchaseSuccess,descriptionKey)` | method | populates card UI; Btn_Card → ShopItemConfirm popup with descriptionKey |
| `RankingCard.Init(rank,displayName,level,rankSprite,avatarSprite,pinned)` | method | populates rank text/medal image, display name fallback, localized rank.level, level/value, avatar, and pinned background |
| `ShopItemResultModel.Success` | field | bool |
| `ShopItemResultModel.ErrorMessage` | field | string; shown on failure |
| `ShopItemResultPopup.Init(ShopItemResultModel)` | method | updates Txt_Title with localized success/fail key; shows error text on failure |
| `ShopInventoryStrip.Refresh(IUiDataService,IStaticCatalogService,Sprite[])` | method | fetches inventory, renders all 4 items with count; dims alpha when count=0 |
| `UIIconAnimator.intervalSeconds` | field | [SerializeField] default 5 s; controls bounce+glow cycle period |
| `UIIconAnimator.EnsureGlow()` | method | creates `Img_Glow` child (RectTransform offset ±14, same sprite, `ProjectLink/UIGlow` additive shader, alpha 0) at runtime Awake |

## Cross-refs
- Consumed by: client `Core.PopupManager`
- Depends on: client `Core.GameContext`, client `Core.UiEventBus`, client `Services.UiScreenViewModels`, `shared/datas/string/`

## Rules
- Namespace: `ProjectLink.OutGame.UI`.
- Scene navigation uses `SceneLoader.LoadScene`; shared state uses `GameContext`.
- Popup and lobby controllers expose serialized refs for Inspector assignment and fallback-find by child name.
- Visible runtime-created strings must use `LocalizedText` or `LocalizationManager.Get(key)` with client string IDs; never hardcode user-facing Korean/EN text. Dynamic labels (e.g. streak button) use `LocalizationManager.Get` directly; static labels use `LocalizedText` component.
- StreakChallengePopup strings: `streak.activate`, `streak.start_level`, `streak.claim`, `streak.level_progress_fmt`, `streak.grand_prize`, `streak.remaining_fmt`, `streak.info_title`, `streak.info_body`, reward format keys.
- StreakChallengePopup sprite refs come from UIBuilder/UISpriteSkin keys: `btn_icon_info`, `slot_streak_banner`, `slot_streak_time_badge`, `slot_streak_prize_panel`, `slot_streak_info_panel`, `slot_streak_path_line`, `slot_streak_path_node`, `slot_streak_path_node_done`, `slot_streak_platform`, `slot_streak_reward_item`, `btn_streak_claim`.
- Stamina timer text: `status.stamina_full` when full; else "MM:SS" countdown. Key added to clientstring.csv.
- StageDetailPopup title: dynamic via `popup.stage.title_n_fmt` (format arg: stageId int). LocalizedText component disabled on Txt_Title before setting text.
- "Guest" display name: use `LocalizationManager.Get("popup.account.guest")` (AccountPopup, SettingPopup, StageDetailPopup ranking display name fallback).
- Ranking tab strings: `rank.title`, `rank.level`; top 1/2/3 sprites come from UISpriteSkin `slot_rank_medal_1/2/3`, card/frame keys from `slot_ranking_card` and `slot_rank_avatar_frame`.
- Toggle visuals: single `Img_Toggle` child of the Toggle transform; sprite swaps between `[SerializeField] toggleOnSprite`/`toggleOffSprite` (assigned by UIBuilder from UISpriteSkin `slot_toggle_on`/`slot_toggle_off`). No `Img_Off`/`Img_On`/`Handle`/`Track` children.
- LobbyTabController.SetTabVisual: Indicator find is null-safe; Indicator no longer exists in generated TabBar.
