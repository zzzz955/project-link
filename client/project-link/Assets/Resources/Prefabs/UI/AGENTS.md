# Resources/Prefabs/UI - Popup prefabs

## Files
| file | class | role |
|---|---|---|
| `SettingPopup.prefab` | `SettingPopup` | Generated prefab with `UI/SettingPopup` image and button hotspots |
| `BuyItemPopup.prefab` | `BuyItemPopup` | Generated prefab with `UI/BuyItemPopup` image and button hotspots |
| `EnergyPopup.prefab` | `EnergyPopup` | Generated prefab with `UI/EnergyPopup` image and button hotspots |

## Symbols
| symbol | kind | note |
|---|---|---|
| `Prefabs/UI/SettingPopup` | Resources path | loaded by `PopupManager` for `PopupId.Settings` |
| `Prefabs/UI/BuyItemPopup` | Resources path | loaded by `PopupManager` for `PopupId.BuyItem` |
| `Prefabs/UI/EnergyPopup` | Resources path | loaded by `PopupManager` for `PopupId.Energy` |

## Rules
- Regenerate via `Tools/Project Link/UI Build/Build Popup Prefabs`.
- Prefab visuals should be image-backed; use transparent hotspots only for interaction.
