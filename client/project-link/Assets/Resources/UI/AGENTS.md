# Resources/UI - UI sprite resources and visual references

## Files
| file | class | role |
|---|---|---|
| `AssetResource1.png` | SpriteSheet | 4x6 multi-sprite UI icon/button sheet; configured by `ProjectLinkUIBuilder.ConfigureUiTextureImports()` |
| `AssetResource2.png` | SpriteSheet | 3x5 multi-sprite stage/popup sheet; configured by `ProjectLinkUIBuilder.ConfigureUiTextureImports()` |
| `AssetResource3.png` | SpriteSheet | 3x3 multi-sprite button/navigation sheet; configured by `ProjectLinkUIBuilder.ConfigureUiTextureImports()` |
| `bootstrap.png` | Reference | bootstrap/loading layout reference only; not auto-assigned by builders |
| `Title.png` | Reference | title scene layout reference only; not auto-assigned by builders |
| `Lobby.png` | Reference | lobby scene layout reference only; not auto-assigned by builders |
| `Game.png` | Reference | game HUD layout reference only; not auto-assigned by builders |
| `SettingPopup.png` | Reference | settings popup layout reference only; not auto-assigned by builders |
| `BuyItemPopup.png` | Reference | buy item popup layout reference only; not auto-assigned by builders |
| `EnergyPopup.png` | Reference | energy refill popup layout reference only; not auto-assigned by builders |

## Symbols
| symbol | kind | note |
|---|---|---|
| `UI/AssetResource1` | Resources path | load sliced sprites with `Resources.LoadAll<Sprite>()` |
| `UI/AssetResource2` | Resources path | load sliced sprites with `Resources.LoadAll<Sprite>()` |
| `UI/AssetResource3` | Resources path | load sliced sprites with `Resources.LoadAll<Sprite>()` |

## Rules
- Runtime code should not load full scene/popup reference PNGs as UI backgrounds.
- Use generated wireframe slots as assignment points for final sprites.
- AssetResource sheets must stay `SpriteImportMode.Multiple`; run `Tools/Project Link/UI Build/Configure UI Texture Imports` after replacing them.
- Slice names are generated as `AssetResourceN_00`, `AssetResourceN_01`, etc.
- Do not store PSD/AI source files here; keep this folder player-build ready.
