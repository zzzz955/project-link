# Resources/Prefabs/UI - Popup prefabs

## Files
| file | class | role |
|---|---|---|
| `SettingPopup.prefab` | `SettingPopup` | Generated settings popup with toggle/dropdown/save hotspots |
| `BuyItemPopup.prefab` | `BuyItemPopup` | Generated item purchase popup |
| `EnergyPopup.prefab` | `EnergyPopup` | Generated stamina/ad/refill popup |
| `StreakChallengePopup.prefab` | `StreakChallengePopup` | Generated 24H streak challenge popup; banner/timer/level/prize/path/claim layout from UIBuilder |
| `AccountPopup.prefab` | `AccountPopup` | Generated account/profile popup |
| `RewardPopup.prefab` | `RewardPopup` | Generated reward claim popup |
| `DailyReward.prefab` | `DailyRewardPopup` | Generated daily reward popup; static reward data-backed wireframe |
| `ClearPopup.prefab` | `ClearPopup` | Generated stage-clear result popup |
| `ClearNextStageConfirmPopup.prefab` | `ClearNextStageConfirmPopup` | Stage-clear next confirmation popup; runtime fills fallback shell if generated refs are absent |
| `PausePopup.prefab` | `PausePopup` | Generated in-game pause popup |
| `TimeoutPopup.prefab` | `TimeoutPopup` | Generated non-dismissible timeout popup |
| `ReturnTitlePopup.prefab` | `ReturnTitlePopup` | Generated title-return confirmation popup |
| `ExitGamePopup.prefab` | `ExitGamePopup` | Generated app-exit confirmation popup |
| `SessionExpiredPopup.prefab` | `SessionExpiredPopup` | Generated non-dismissible session-expired popup |
| `ForceUpdatePopup.prefab` | `ForceUpdatePopup` | Generated non-dismissible force-update popup |
| `MaintenancePopup.prefab` | `MaintenancePopup` | Generated non-dismissible maintenance popup |
| `StageDetailPopup.prefab` | `StageDetailPopup` | Generated stage detail/start popup |
| `RankingCard.prefab` | `RankingCard` | Generated lobby ranking row card used by Tab_Ranking list and pinned my-rank |

## Symbols
| symbol | kind | note |
|---|---|---|
| `Prefabs/UI/SettingPopup` | Resources path | loaded by `PopupManager` for `PopupId.Settings` |
| `Prefabs/UI/BuyItemPopup` | Resources path | loaded by `PopupManager` for `PopupId.BuyItem` |
| `Prefabs/UI/EnergyPopup` | Resources path | loaded by `PopupManager` for `PopupId.Energy` |
| `Prefabs/UI/StreakChallengePopup` | Resources path | loaded by `PopupManager` for `PopupId.StreakChallenge` |
| `Prefabs/UI/AccountPopup` | Resources path | loaded by `PopupManager` for `PopupId.Account` |
| `Prefabs/UI/RewardPopup` | Resources path | loaded by `PopupManager` for `PopupId.Reward` |
| `Prefabs/UI/DailyReward` | Resources path | loaded by `PopupManager` for `PopupId.DailyReward` |
| `Prefabs/UI/ClearPopup` | Resources path | loaded by `PopupManager` for `PopupId.StageClear` |
| `Prefabs/UI/ClearNextStageConfirmPopup` | Resources path | loaded by `PopupManager` for `PopupId.ClearNextStageConfirm` |
| `Prefabs/UI/PausePopup` | Resources path | loaded by `PopupManager` for `PopupId.Pause` |
| `Prefabs/UI/SessionExpiredPopup` | Resources path | generated session-expired prefab; current fallback may build code-only popup |
| `Prefabs/UI/ForceUpdatePopup` | Resources path | loaded by `PopupManager` for `PopupId.ForceUpdate` |
| `Prefabs/UI/MaintenancePopup` | Resources path | loaded by `PopupManager` for `PopupId.Maintenance` |
| `Prefabs/UI/StageDetailPopup` | Resources path | loaded by `PopupManager` when stage detail flow is wired |
| `Prefabs/UI/RankingCard` | Resources path | assigned by `ProjectLinkUIBuilder` to `LobbyWireframeController.rankingCardPrefab` |

## Rules
- Regenerate via `Tools/Project Link/UI Build/Build Popup Prefabs`.
- Prefab visuals should be image-backed; use transparent hotspots only for interaction.
