# Generated/Contracts - Unity DTO mirror

Unity-compatible copy of `shared/contracts/*.cs` for client API/service binding.

## Files
| file | namespace | role |
|---|---|---|
| `BootstrapResponses.cs` | `ProjectLink.Contracts.Bootstrap` | Bootstrap config response |
| `AccountRequests.cs` | `ProjectLink.Contracts.Account` | Auth/account request DTOs |
| `AccountResponses.cs` | `ProjectLink.Contracts.Account` | Auth/account response DTOs |
| `LobbyResponses.cs` | `ProjectLink.Contracts.Lobby` | Lobby state response DTOs |
| `ProgressRequests.cs` | `ProjectLink.Contracts.Progress` | Progress query request DTOs |
| `ProgressResponses.cs` | `ProjectLink.Contracts.Progress` | Progress list response DTOs |
| `StageRequests.cs` | `ProjectLink.Contracts.Stage` | Stage session request DTOs |
| `StageResponses.cs` | `ProjectLink.Contracts.Stage` | Stage session response DTOs |
| `StaminaRequests.cs` | `ProjectLink.Contracts.Stamina` | Stamina reward/refill request DTOs |
| `StaminaResponses.cs` | `ProjectLink.Contracts.Stamina` | Stamina state/reward response DTOs |
| `CurrencyRequests.cs` | `ProjectLink.Contracts.Currency` | Currency ad reward request DTOs |
| `CurrencyResponses.cs` | `ProjectLink.Contracts.Currency` | Currency balance/reward response DTOs |
| `ItemRequests.cs` | `ProjectLink.Contracts.Item` | Inventory item request DTOs |
| `ItemResponses.cs` | `ProjectLink.Contracts.Item` | Inventory item response DTOs |
| `RankingResponses.cs` | `ProjectLink.Contracts.Ranking` | Ranking list/my-rank response DTOs |
| `DailyChallengeRequests.cs` | `ProjectLink.Contracts.Daily` | Daily challenge request DTOs |
| `DailyChallengeResponses.cs` | `ProjectLink.Contracts.Daily` | Daily challenge response DTOs |
| `RewardRequests.cs` | `ProjectLink.Contracts.Reward` | Reward claim request DTOs |
| `RewardResponses.cs` | `ProjectLink.Contracts.Reward` | Reward claim response DTOs |
| `ShopRequests.cs` | `ProjectLink.Contracts.Shop` | Shop purchase request DTOs |
| `ShopResponses.cs` | `ProjectLink.Contracts.Shop` | Shop catalog/purchase response DTOs |
| `SeasonEventResponses.cs` | `ProjectLink.Contracts.Event` | Season event response DTOs |
| `PlayerSettingsRequests.cs` | `ProjectLink.Contracts.Settings` | Player settings update request DTOs |
| `PlayerSettingsResponses.cs` | `ProjectLink.Contracts.Settings` | Player settings response DTOs |
| `ErrorResponse.cs` | `ProjectLink.Contracts.Common` | Unified error response DTO |

## Symbols
| symbol | kind | note |
|---|---|---|
| `BootstrapConfigResponse` | class | server config/version response |
| `AccountMeResponse` | class | account/profile response |
| `LobbyStateResponse` | class | aggregate lobby snapshot |
| `ProgressResponse` | class | stage progress entries |
| `StageStartResponse` | class | stage session start response |
| `StageEndResponse` | class | stage clear/fail result response |
| `StaminaResponse` | class | stamina current/max/next recharge |
| `CurrencyResponse` | class | soft currency balance |
| `InventoryResponse` | class | item inventory slots |
| `RankingListResponse` | class | leaderboard rows and my pinned rank |
| `DailyChallengeResponse` | class | daily state, tiles, rewards |
| `RewardClaimResponse` | class | granted reward result |
| `ShopCatalogResponse` | class | shop catalog and soft balance |
| `ActiveEventsResponse` | class | season event list |
| `PlayerSettingsResponse` | class | persisted settings state |

## Cross-refs
- Consumed by: client `Services.IUiDataService`
- Depends on: `shared/contracts/AGENTS.md`

## Rules
- DTOs here mirror `shared/contracts`; do not add client-only logic.
- Keep syntax compatible with Unity C# 9.0: no file-scoped namespaces, global usings, or C# 10+ features.
- Keep source `#nullable enable` headers when refreshing mirrors.
- Refresh by copying source contract `.cs` files after server DTO changes.
- Keep this AGENTS file in sync when shared contract files are added/removed.
