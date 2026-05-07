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
| `ProjectLinkUIBuilder.BuildPopupPrefabs()` | method | [MenuItem] creates `Assets/Resources/Prefabs/UI/*.prefab` with image slots and transparent hotspots |
| `ProjectLinkUIBuilder.ConfigureUiTextureImports()` | method | configures AssetResource sheets as Multiple sprites via SpriteDataProvider |

## Rules
- Editor-only folder; auto-excluded from player builds (Unity convention)
- Scene builders must be idempotent: destroy existing generated roots before recreating
- Stable root object names required for clean diffs
- Scene/popup builders must not assign full-screen reference PNGs; create RectTransform image slots and transparent Button hotspots only.
- Never touch `Assets/Resources/Data/` or `Assets/Scripts/Data/Generated/`
