# Editor — Unity Editor automation tools

## Files
| file | class | role |
|---|---|---|
| `ProjectLinkUIBuilder.cs` | `ProjectLinkUIBuilder` | [MenuItem] wireframe scene UI, popup prefab builder, AssetResource import/slicing setup; applies UIButtonSkin sprites when present |
| `ProjectLinkUIImageResourceExtractor.cs` | `ProjectLinkUIImageResourceExtractor` | [MenuItem] batch-loads transparent UI images in a scrollable editor window, parses alpha-connected resource elements, previews them, saves numbered PNG sprites |
| `UIButtonSkin.cs` | `UIButtonSkin` | ScriptableObject; maps element names → Sprite for buttons and image slots; asset lives at `Assets/Editor/UIButtonSkin.asset` |

## Symbols
| symbol | kind | note |
|---|---|---|
| `ProjectLinkUIBuilder.BuildCurrentSceneUI()` | method | [MenuItem] rebuilds active scene UI matching `scenes.json` spec |
| `ProjectLinkUIBuilder.BuildAllSceneUI()` | method | [MenuItem] rebuilds all scenes + popup prefabs matching design spec |
| `ProjectLinkUIBuilder.BuildAllSceneUIBatch()` | method | CI/batch variant; no dialogs |
| `ProjectLinkUIBuilder.BuildPopupPrefabs()` | method | [MenuItem] creates all popup prefabs under `Assets/Resources/Prefabs/UI/` using standard popup shell (Overlay/Panel/Header/Content/Footer) |
| `ProjectLinkUIBuilder.CreateUIButtonSkin()` | method | [MenuItem] creates `Assets/Editor/UIButtonSkin.asset` if absent |
| `ProjectLinkUIBuilder.ConfigureUiTextureImports()` | method | configures AssetResource sheets as Multiple sprites via SpriteDataProvider |
| `ProjectLinkUIBuilder.CreatePopupShell<T>(...)` | method | creates standard popup shell: Overlay + Panel(Header+Divider+Content+Divider+Footer); dismissible controls Overlay button and Btn_Close presence |
| `ProjectLinkUIBuilder.BuildBootstrap(...)` | method | Slot_Logo + ProgressBar(Slider+Fill) + Btn_Retry + Txt_NetworkError + PopupLayer; wires BootstrapWireframeController |
| `ProjectLinkUIBuilder.BuildTitle(...)` | method | Btn_Settings + Slot_Logo + Group_AuthButtons(Btn_Google+Btn_Apple) + Btn_TapToStart + Txt_Version; wires TitleWireframeController |
| `ProjectLinkUIBuilder.BuildLobby(...)` | method | HUD_Strip(Row_Profile+Row_Stats) + MenuDropdown + Group_TabBodies(Tab_Home+Tab_Shop+Tab_Ranking) + TabBar; wires LobbyWireframeController + LobbyTabController |
| `ProjectLinkUIBuilder.BuildGame(...)` | method | HUD_Top(Row_TopBar+Row_Objectives) + Toolbar_Items only; board renders in camera/world space; wires GameWireframeController |
| `ProjectLinkUIBuilder.ApplyButtonSkin(image, key)` | method | assigns sprite from UIButtonSkin.buttons by skin-key; sets color white when matched |
| `ProjectLinkUIBuilder.ApplySlotSkin(image, key)` | method | assigns sprite from UIButtonSkin.imageSlots by skin-key; sets color white when matched |
| `ProjectLinkUIBuilder.FindTmpInChildren(root, name)` | method | linear search; returns first TextMeshProUGUI whose name matches |
| `ProjectLinkUIBuilder.FindRectInChildren(root, name)` | method | linear search; returns first RectTransform whose name matches |
| `UIButtonSkin.GetButton(name)` | method | linear search over buttons entries; returns null if not found |
| `UIButtonSkin.GetSlot(name)` | method | linear search over imageSlots entries; returns null if not found |
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
- Scene/popup builders create wireframe placeholders by default; actual sprites are applied only when `UIButtonSkin.asset` is present and has an entry for the element name.
- `UIButtonSkin.asset` lives in `Assets/Editor/` (never `Assets/Resources/`) to exclude it from player builds.
- Prefer anchors/layout groups/ScrollRect over fixed-only placement for generated UI hierarchy.
- Never touch `Assets/Resources/Data/` or `Assets/Scripts/Data/Generated/`
