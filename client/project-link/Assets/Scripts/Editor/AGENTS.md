# Editor — Unity Editor automation tools

## Files
| file | class | role |
|---|---|---|
| `ProjectLinkUIBuilder.cs` | `ProjectLinkUIBuilder` | [MenuItem] wireframe scene UI, popup prefab builder, AssetResource import/slicing setup |

## Symbols
| symbol | kind | note |
|---|---|---|
| `ProjectLinkUIBuilder.BuildCurrentSceneUI()` | method | [MenuItem] rebuilds active scene runtime UI |
| `ProjectLinkUIBuilder.BuildAllSceneUI()` | method | [MenuItem] rebuilds wireframe UI hierarchy for all game scenes and popup prefabs |
| `ProjectLinkUIBuilder.BuildAllSceneUIBatch()` | method | CI/batch variant; no dialogs |
| `ProjectLinkUIBuilder.BuildPopupPrefabs()` | method | [MenuItem] creates `Assets/Resources/Prefabs/UI/*.prefab` including ClearPopup with visible image slots, buttons, and serialized controller refs |
| `ProjectLinkUIBuilder.ConfigureUiTextureImports()` | method | configures AssetResource sheets as Multiple sprites via SpriteDataProvider |
| `ProjectLinkUIBuilder.DecoratePopup(...)` | method | adds PDF text slots and bindable labels to generated popup prefabs |
| `ProjectLinkUIBuilder.AddScrollView(...)` | method | creates visible ScrollRect placeholder with Viewport/Content/items |
| `ProjectLinkUIBuilder.AddLobbyTabController(...)` | method | attaches LobbyTabController by type name and assigns serialized refs |
| `ProjectLinkUIBuilder.BuildBootstrap(...)` | method | attaches BootstrapWireframeController and assigns serialized refs |
| `ProjectLinkUIBuilder.BuildTitle(...)` | method | attaches TitleWireframeController and assigns serialized refs |
| `ProjectLinkUIBuilder.BuildGameShell(...)` | method | attaches GameWireframeController and assigns serialized refs |

## Cross-refs
- Consumed by: Unity Editor only (not included in player builds)
- Depends on: client `Core` scene/popup structure (BuildAllSceneUI/BuildPopupPrefabs reconstruct from scratch)

## Rules
- Editor-only folder; auto-excluded from player builds (Unity convention)
- Scene builders must be idempotent: destroy existing generated roots before recreating
- Stable root object names required for clean diffs
- Scene/popup builders must not assign full-screen reference PNGs; create visible RectTransform image slots and Button hotspots only.
- Prefer anchors/layout groups/ScrollRect over fixed-only placement for generated UI hierarchy.
- Never touch `Assets/Resources/Data/` or `Assets/Scripts/Data/Generated/`
