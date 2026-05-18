# Editor — Unity Editor automation tools

## Files
| file | class | role |
|---|---|---|
| `ProjectLinkUIBuilder.cs` | `ProjectLinkUIBuilder` | [MenuItem] wireframe scene UI, popup prefab builder, AssetResource import/slicing setup; applies UIButtonSkin sprites when present |
| `ProjectLinkUIImageResourceExtractor.cs` | `ProjectLinkUIImageResourceExtractor` | [MenuItem] batch-loads transparent UI images in a scrollable editor window, parses alpha-connected resource elements, previews them, saves numbered PNG sprites |
| `ClearPlayerPrefs.cs` | `PlayerPrefsResetMenu` | [MenuItem] clears all PlayerPrefs for local reset/debug |
| `UISpriteSkin.cs` | `UISpriteSkin` | ScriptableObject; maps element names → Sprite for all static UI images; asset lives at `Assets/Editor/UISpriteSkin.asset` |

## Symbols
| symbol | kind | note |
|---|---|---|
| `ProjectLinkUIBuilder.BuildCurrentSceneUI()` | method | [MenuItem] rebuilds active scene UI matching `scenes.json` spec |
| `ProjectLinkUIBuilder.BuildAllSceneUI()` | method | [MenuItem] rebuilds all scenes + popup prefabs matching design spec |
| `ProjectLinkUIBuilder.BuildAllSceneUIBatch()` | method | CI/batch variant; no dialogs |
| `ProjectLinkUIBuilder.BuildPopupPrefabs()` | method | [MenuItem] creates all popup prefabs under `Assets/Resources/Prefabs/UI/` using standard popup shell (Overlay/Panel/Header/Content/Footer), including clear-next confirmation |
| `ProjectLinkUIBuilder.CreateUISpriteSkin()` | method | [MenuItem] creates/syncs `Assets/Editor/UISpriteSkin.asset`; scans source for `btn_*`/`slot_*` keys automatically |
| `ProjectLinkUIBuilder.AssignIconAnimations()` | method | [MenuItem] scans all scenes + popup prefabs; adds `UIIconAnimator` to every `Icon_*`/`Icon` GO and any Image using a `btn_icon_*` skin key; auto-called from BuildScene/SavePopupPrefab |
| `ProjectLinkUIBuilder.RegisterUntrackedSprites()` | method | [MenuItem] scans all scenes + popup prefabs for Image.sprite not tracked in UISpriteSkin; groups by Sprite reference; derives `slot_*`/`btn_*` keys and adds entries to UISpriteSkin.asset |
| `ProjectLinkUIBuilder.AddAnimatorToIconImages(root)` | method | adds `UIIconAnimator` to icon images in a GO hierarchy; used by BuildScene/SavePopupPrefab/AssignIconAnimations |
| `ProjectLinkUIBuilder.DeriveKey(goName,parentName,existingKeys)` | method | converts a GO name to a UISpriteSkin key (`slot_*`/`btn_*`); disambiguates with parent name or numeric suffix |
| `ProjectLinkUIBuilder.ConfigureUiTextureImports()` | method | configures AssetResource sheets as Multiple sprites via SpriteDataProvider |
| `ProjectLinkUIBuilder.CreatePopupShell<T>(...)` | method | creates standard popup shell: Overlay + Panel(Header+Divider+Content+Divider+Footer); Header: no HLG; Txt_Title Stretch full-width MidlineCenter; Btn_Close anchor (1,0.5) anchoredPosition (-24,0); dismissible controls Overlay button and Btn_Close; `PopupShape` param selects background skin key |
| `ProjectLinkUIBuilder.EnsureLocalizedFonts(root)` | method | adds `LocalizedFont` component to every TMP child that lacks `LocalizedText`; called in `SavePopupPrefab` and at end of `BuildScene`; idempotent (skips if already present) |
| `ProjectLinkUIBuilder.BuildBootstrap(...)` | method | Slot_Logo + ProgressBar(Slider+Fill) + Btn_Retry + Txt_NetworkError + PopupLayer; wires BootstrapWireframeController |
| `ProjectLinkUIBuilder.BuildTitle(...)` | method | Btn_Settings + Slot_Logo + Group_AuthButtons(Btn_Google+Btn_Apple) + Btn_TapToStart + Txt_Version; wires TitleWireframeController |
| `ProjectLinkUIBuilder.BuildLobby(...)` | method | HUD_Strip(single HLG h=120 transparent: Slot_Avatar+Group_Stamina+Group_Currency+Btn_Menu) + MenuDropdown + Group_TabBodies(Tab_Home+Tab_Shop+Tab_Ranking) + TabBar; wires LobbyWireframeController + LobbyTabController |
| `ProjectLinkUIBuilder.AddStaminaGroup(hud,router)` | method | Group_Stamina (flexibleWidth=1, h=80) + slot_resource_bg bg Image + Stack_Stamina(Icon_Stamina+Txt_StaminaCount overlay) + Txt_StaminaTimer(MidlineLeft) |
| `ProjectLinkUIBuilder.AddCurrencyGroup(hud)` | method | Group_Currency (w=160, h=80) + slot_resource_bg bg Image + Icon_Currency + Txt_CurrencyCount(MidlineLeft) |
| `ProjectLinkUIBuilder.BuildGame(...)` | method | HUD_Top(Row_TopBar+Row_Objectives) + Toolbar_Items (4 ItemSlot_1..4 each with Img_Icon+Txt_Count+Button); assigns item1..4Button and item1..4CountText to GameWireframeController; no persistent onClick listener (InGameHUD.InitItemToolbar handles binding) |
| `ProjectLinkUIBuilder.NormalizeLayoutText(...)` | method | adds layout sizing to TMP children under LayoutGroups so generated text width is not zero |
| `ProjectLinkUIBuilder.ApplySkin(image, key, preserveAspect)` | method | assigns sprite from UISpriteSkin.sprites by skin-key; sets color white; preserveAspect=true by default, pass false for stretch-fill backgrounds (slot_popup_bg, slot_popup_overlay, slot_hud_bg, slot_progress_track, slot_resource_bg); skin keys: slot_resource_bg (Group_Stamina/Group_Currency bg), slot_loading_spinner (SceneLoader spinner — place in Assets/Resources/UI/), slot_loading_bg (SceneLoader overlay bg) |
| `ProjectLinkUIBuilder.FindTmpInChildren(root, name)` | method | linear search; returns first TextMeshProUGUI whose name matches |
| `ProjectLinkUIBuilder.FindRectInChildren(root, name)` | method | linear search; returns first RectTransform whose name matches |
| `PlayerPrefsResetMenu.ResetPrefs()` | method | [MenuItem] deletes all PlayerPrefs and saves immediately |
| `UISpriteSkin.Get(name)` | method | linear search over sprites entries; returns null if not found |
| `ProjectLinkUIImageResourceExtractor.Open()` | method | [MenuItem] opens the UI resource extraction editor window |
| `ProjectLinkUIImageResourceExtractor.ParseAllSources()` | method | parses every added image into alpha-connected resource components |
| `ProjectLinkUIImageResourceExtractor.AddDraggedItems()` | method | accepts multiple dragged Texture assets, image files, or image folders |
| `ProjectLinkUIImageResourceExtractor.SaveResources()` | method | saves parsed previews as `baseFileName_1.png` style PNG files |

## Cross-refs
- Consumed by: Unity Editor only (not included in player builds)
- Depends on: client `Core` scene/popup structure (BuildAllSceneUI/BuildPopupPrefabs reconstruct from scratch)

## Rules
- Editor-only folder; auto-excluded from player builds (Unity convention)
- Scene builders must be idempotent: destroy existing generated roots before recreating
- Stable root object names required for clean diffs
- Scene/popup builders create wireframe placeholders by default; actual sprites are applied only when `UISpriteSkin.asset` is present and has an entry for the element name.
- `UISpriteSkin.asset` lives in `Assets/Editor/` (never `Assets/Resources/`) to exclude it from player builds.
- Prefer anchors/layout groups/ScrollRect over fixed-only placement for generated UI hierarchy.
- Never touch `Assets/Resources/Data/` or `Assets/Scripts/Data/Generated/`
- All builder-created TMP elements receive `LocalizedFont` component via `EnsureLocalizedFonts` (called in `SavePopupPrefab` and `BuildScene`); static labels with `stringId` use `LocalizedText` (handles both text + font); dynamic labels use `LocalizedFont` (font only). Never leave TMP with default LiberationSans SDF.
- Popup header: no HLG. `Txt_Title` stretches full-width (symmetric left/right margins when dismissible) with `MidlineCenter` alignment. `Btn_Close` anchored (1,0.5) with anchoredPosition (-24,0).
- Toggle rows use a single child Image `Img_Toggle` (skin key: `slot_toggle_off` at build time); no `Handle`/`Track`/`Img_On`/`Img_Off` children. Toggle component uses `Transition.None`. Runtime `SettingPopup` swaps `Img_Toggle` sprite between `slot_toggle_off`/`slot_toggle_on` sprites on value change with a scale-compress → bounce animation.
- TabBar buttons: no `Indicator` child. Selected state driven entirely by `LobbyTabController.SetTabVisual` (bold + scale). VLG has 8 px bottom padding to prevent text descender clipping.
- All builder-created TMP center alignment uses `TextAlignmentOptions.Midline` (geometry center, TMP 3.x); left/right variants use `MidlineLeft`/`MidlineRight`. Never use bare `Center` (= bounding-box Middle, visually different).
- Star images in popup Group_Stars use skin keys `slot_star_on` (earned) / `slot_star_off` (empty). Builder pre-creates three `Img_Star_0/1/2` Image slots; runtime popups update them in-place or fallback to dynamic creation. Popups expose `[SerializeField] Sprite starOnSprite, starOffSprite` assigned by builder from UISpriteSkin.
