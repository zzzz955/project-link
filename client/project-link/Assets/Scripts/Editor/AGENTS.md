# Editor — Unity Editor automation tools

## Files
| file | class | role |
|---|---|---|
| `ProjectLinkUIBuilder.cs` | `ProjectLinkUIBuilder` | [MenuItem] wireframe scene UI, popup prefab builder, AssetResource import/slicing setup; applies UIButtonSkin sprites when present |
| `ProjectLinkUIImageResourceExtractor.cs` | `ProjectLinkUIImageResourceExtractor` | [MenuItem] batch-loads transparent UI images in a scrollable editor window, parses alpha-connected resource elements, previews them, saves numbered PNG sprites |
| `ClearPlayerPrefs.cs` | `PlayerPrefsResetMenu` | [MenuItem] clears all PlayerPrefs for local reset/debug |
| `UISpriteSkin.cs` | `UISpriteSkin` | ScriptableObject; maps element names → Sprite for all static UI images; asset lives at `Assets/Editor/UISpriteSkin.asset` |
| `UIBaselineSnapshot.cs` | `UIBaselineSnapshot` | ScriptableObject; clean-build hierarchy snapshot (target/path/comp/key→val); `Assets/Editor/UIBaselineSnapshot.asset` |
| `UIOverrideManifest.cs` | `UIOverrideManifest` | ScriptableObject; pending/promoted override entries; also writes `Assets/Editor/UIOverrideManifest.json` for AI agent |
| `UIPropertySerializer.cs` | `UIPropertySerializer` | Static; get/set tracked component properties (RectTransform, Image, TMP, LayoutGroup, etc.) as strings |
| `ProjectLinkUIOverrideCapture.cs` | `ProjectLinkUIOverrideCapture` | [MenuItem] CaptureAllOverrides, ClearPromoted |
| `ProjectLinkUIOverrideCapture.cs` | `ProjectLinkUIOverrideApply` | Called by Builder; SnapshotAndApplyScene/Prefab — save baseline then re-apply pending prop overrides |

## Symbols
| symbol | kind | note |
|---|---|---|
| `ProjectLinkUIBuilder.BuildCurrentSceneUI()` | method | [MenuItem] rebuilds active scene UI matching `scenes.json` spec |
| `ProjectLinkUIBuilder.BuildAllSceneUI()` | method | [MenuItem] rebuilds all scenes + popup prefabs matching design spec; restores unchanged files to avoid fileID churn in git diff |
| `ProjectLinkUIBuilder.ContentMatchesIgnoringFileIds(newContent,oldContent)` | method | splits YAML into per-object blocks, replaces all fileID integers with stable placeholder, sorts blocks, compares; order-independent so random fileID sort order doesn't cause false positives |
| `ProjectLinkUIBuilder.GetSortedBlockSignatures(yaml)` | method | splits YAML on `\n--- ` separator, normalizes `&NNNNN` anchors and `fileID: NNNNN` refs to `&ID`/`fileID: ID`, returns sorted list |
| `ProjectLinkUIBuilder.RestoreIfUnchanged(path,previousContent)` | method | writes previousContent back to disk + ImportAsset if ContentMatchesIgnoringFileIds match; suppresses git diff noise |
| `ProjectLinkUIBuilder.BuildAllSceneUIBatch()` | method | CI/batch variant; no dialogs |
| `ProjectLinkUIBuilder.BuildPopupPrefabs()` | method | [MenuItem] creates all popup prefabs under `Assets/Resources/Prefabs/UI/` using standard popup shell (Overlay/Panel/Header/Content/Footer), including clear-next confirmation |
| `ProjectLinkUIBuilder.CreateRankingCardPrefab()` | method | creates `RankingCard.prefab` with rank/medal slot, avatar frame, display name, and localized level/value labels |
| `ProjectLinkUIBuilder.BuildStreakChallengePopup()` | method | creates StreakChallenge popup hierarchy with Btn_Info/Btn_Close, event banner, HHh MMm timer, level progress, grand-prize panel, dynamic LevelPath root, claim action, and hidden InfoPopup |
| `ProjectLinkUIBuilder.CreateUISpriteSkin()` | method | [MenuItem] creates/syncs `Assets/Editor/UISpriteSkin.asset`; scans source for `btn_*`/`slot_*` keys automatically |
| `ProjectLinkUIBuilder.AssignIconAnimations()` | method | [MenuItem] scans all scenes + popup prefabs; adds `UIIconAnimator` to every `Icon_*`/`Icon` GO and any Image using a `btn_icon_*` skin key; auto-called from BuildScene/SavePopupPrefab |
| `ProjectLinkUIBuilder.RegisterUntrackedSprites()` | method | [MenuItem] scans all scenes + popup prefabs for Image.sprite not tracked in UISpriteSkin; groups by Sprite reference; derives `slot_*`/`btn_*` keys and adds entries to UISpriteSkin.asset |
| `ProjectLinkUIBuilder.AddAnimatorToIconImages(root)` | method | adds `UIIconAnimator` to icon images in a GO hierarchy; used by BuildScene/SavePopupPrefab/AssignIconAnimations |
| `ProjectLinkUIBuilder.DeriveKey(goName,parentName,existingKeys)` | method | converts a GO name to a UISpriteSkin key (`slot_*`/`btn_*`); disambiguates with parent name or numeric suffix |
| `ProjectLinkUIBuilder.ConfigureUiTextureImports()` | method | configures AssetResource sheets as Multiple sprites via SpriteDataProvider |
| `ProjectLinkUIBuilder.CreatePopupShell<T>(...)` | method | creates standard popup shell: Overlay + Panel(Header+Divider+Content+Divider+Footer); Header: no HLG; Txt_Title Stretch full-width MidlineCenter; Btn_Close anchor (1,0.5) anchoredPosition (-24,0); dismissible controls Overlay button and Btn_Close; `PopupShape` param selects background skin key |
| `ProjectLinkUIBuilder.EnsureLocalizedFonts(root)` | method | adds `LocalizedFont` component to every TMP child that lacks `LocalizedText`; called in `SavePopupPrefab` and at end of `BuildScene`; idempotent (skips if already present) |
| `ProjectLinkUIBuilder.BuildBootstrap(...)` | method | Slot_Logo + Txt_Loading(status) + ProgressBar(Slider+Fill) + Txt_Version + Btn_Retry + Txt_NetworkError + PopupLayer; wires all 5 BootstrapWireframeController refs |
| `ProjectLinkUIBuilder.BuildTitle(...)` | method | Btn_Settings + Slot_Logo + Group_AuthButtons(Btn_Google+Btn_Apple) + Btn_TapToStart + Txt_Version; wires TitleWireframeController |
| `ProjectLinkUIBuilder.BuildLobby(...)` | method | HUD_Strip(single HLG h=120 transparent: Slot_Avatar+Group_Stamina+Group_Currency+Btn_Menu) + MenuDropdown + Group_TabBodies(Tab_Home+Tab_Shop+Tab_Ranking) + TabBar; wires LobbyWireframeController + LobbyTabController, including RankingCard prefab refs |
| `ProjectLinkUIBuilder.BuildRankingTab(...)` | method | creates Ranking title, existing ranking segment buttons, scroll content, and pinned my-rank container |
| `ProjectLinkUIBuilder.AddStaminaGroup(hud,router)` | method | Group_Stamina (flexibleWidth=1, h=80) + slot_resource_bg bg Image + Stack_Stamina(Icon_Stamina+Txt_StaminaCount overlay) + Txt_StaminaTimer(MidlineLeft) |
| `ProjectLinkUIBuilder.AddCurrencyGroup(hud)` | method | Group_Currency (w=160, h=80) + slot_resource_bg bg Image + Icon_Currency + Txt_CurrencyCount(MidlineLeft) |
| `ProjectLinkUIBuilder.BuildGame(...)` | method | HUD_Top(Row_TopBar+Row_Objectives) + Toolbar_Items (4 ItemSlot_1..4 each with Img_Icon+Txt_Count+Button); Row_Objectives contains Txt_Pipe (pipe counter, flexibleWidth=1) + Txt_Moves; assigns pipeCounterText, levelLabelText, moveCounterText, item1..4Button and item1..4CountText to GameWireframeController; no persistent onClick listener (InGameHUD.InitItemToolbar handles binding) |
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
| `ProjectLinkUIOverrideCapture.CaptureAllOverrides()` | method | [MenuItem] diffs all scenes+prefabs vs baseline → writes manifest; replaces all pending entries fresh each run |
| `ProjectLinkUIOverrideCapture.ClearPromoted()` | method | [MenuItem] removes entries with status="promoted" from manifest |
| `ProjectLinkUIOverrideApply.SnapshotAndApplyScene(sceneName)` | method | called by BuildScene; saves clean-build baseline then applies pending prop overrides to active scene |
| `ProjectLinkUIOverrideApply.SnapshotAndApplyPrefab(root,prefabName)` | method | called by SavePopupPrefab; saves clean-build baseline then applies pending prop overrides to in-memory prefab root |
| `UIBaselineSnapshot.TryGet(target,path,comp,key,val)` | method | lookup in index; returns false if not found |
| `UIBaselineSnapshot.ContainsPath(target,path)` | method | true if any record exists for target+path (new_go detection) |
| `UIBaselineSnapshot.GetPathsForTarget(target)` | method | returns all GO paths in baseline for a target (remove_go detection) |
| `UIOverrideManifest.WriteJson()` | method | writes `Assets/Editor/UIOverrideManifest.json` — AI-readable manifest with id/target/method/path/op/comp/key/baseVal/currVal/status |
| `UIPropertySerializer.Get(comp,key)` | method | serializes component property to string; returns null if comp/key not tracked |
| `UIPropertySerializer.Set(comp,key,val)` | method | deserializes and applies string value to component property; returns false on failure |

## Cross-refs
- Consumed by: Unity Editor only (not included in player builds)
- Depends on: client `Core` scene/popup structure (BuildAllSceneUI/BuildPopupPrefabs reconstruct from scratch)

## Rules
- Editor-only folder; auto-excluded from player builds (Unity convention)
- Scene builders must be idempotent: destroy existing generated roots before recreating
- Stable root object names required for clean diffs
- BuildAllSceneUI / SavePopupPrefab restore original file when only fileIDs changed (post-build YAML normalization); git diff only shows real structural changes
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
- Ranking tab/cards use UISpriteSkin keys `slot_ranking_card`, `slot_rank_avatar_frame`, `slot_rank_segment_bg`, `slot_rank_segment_selected`, `slot_rank_segment_idle`, and `slot_rank_medal_1/2/3`.
- UI Override system: BuildAllSceneUI saves clean-build baseline then re-applies manifest overrides — manual UI changes persist across builds automatically (prop ops only). new_go/remove_go ops require AI or manual promotion to builder code.
- Override workflow: manual edit → CaptureAllOverrides → manifest updated → AI reads manifest+method hint → updates builder code → marks entry promoted → ClearPromoted.
- Baseline (UIBaselineSnapshot.asset) + Manifest (UIOverrideManifest.asset/json) live in `Assets/Editor/`; never edit them directly.
