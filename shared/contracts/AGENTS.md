# shared/contracts — Shared C# DTO Contracts

## Overview
Language-agnostic DTO definitions written directly in C#.
Both server (ASP.NET Core) and client (Unity) consume these classes.

## Project
`ProjectLink.Contracts.csproj` — targets `netstandard2.1` (Unity compatible)

## Structure
```
shared/contracts/
  [Domain]/
    [Domain]Requests.cs    e.g. StageRequests.cs
    [Domain]Responses.cs   e.g. StageResponses.cs
```

## Rules
- Target `netstandard2.1` — no net8.0-only APIs
- Keep syntax compatible with Unity C# 9.0: no file-scoped namespaces, global usings, or C# 10+ features.
- Put `#nullable enable` in each contract file because Unity assembly nullable context is not project-wide.
- Only POCOs: public properties, no logic, no dependencies
- Nullable enabled — use `Type?` for optional fields
- Namespace: `ProjectLink.Contracts.[Domain]`
- Server: references via `ProjectReference` in `ProjectLink.API.csproj`
- Unity: copy `.cs` files to `client/project-link/Assets/Scripts/Generated/Contracts/`

## Adding a new domain
1. Create `[Domain]/` subdirectory
2. Add `[Domain]Requests.cs` and/or `[Domain]Responses.cs`
3. Copy updated `.cs` files to Unity Assets (manual or via script)
4. Update `## Files` and `## Symbols` below

## Files
| file | namespace | role |
|------|-----------|------|
| `Common/ErrorResponse.cs` | `ProjectLink.Contracts.Common` | Unified error response — `ErrorCode` string maps to `error_messages.csv` |
| `Bootstrap/BootstrapResponses.cs` | `ProjectLink.Contracts.Bootstrap` | Client version + server time response (no auth) |
| `Account/AccountRequests.cs` | `ProjectLink.Contracts.Account` | Guest/social login, refresh, link account |
| `Account/AccountResponses.cs` | `ProjectLink.Contracts.Account` | Auth token pair + profile |
| `Stage/StageRequests.cs` | `ProjectLink.Contracts.Stage` | Stage start/end request bodies |
| `Stage/StageResponses.cs` | `ProjectLink.Contracts.Stage` | Stage start/end response shapes |
| `Stamina/StaminaRequests.cs` | `ProjectLink.Contracts.Stamina` | Ad-reward, extend (legacy), and refill request bodies |
| `Stamina/StaminaResponses.cs` | `ProjectLink.Contracts.Stamina` | Stamina state and reward responses |
| `Currency/CurrencyRequests.cs` | `ProjectLink.Contracts.Currency` | Ad-reward request body |
| `Currency/CurrencyResponses.cs` | `ProjectLink.Contracts.Currency` | Balance and reward responses |
| `Item/ItemRequests.cs` | `ProjectLink.Contracts.Item` | Purchase and use request bodies |
| `Item/ItemResponses.cs` | `ProjectLink.Contracts.Item` | Inventory and purchase responses |
| `Ranking/RankingResponses.cs` | `ProjectLink.Contracts.Ranking` | Ranking list and my-rank responses |
| `Lobby/LobbyResponses.cs` | `ProjectLink.Contracts.Lobby` | Full lobby state snapshot |
| `Progress/ProgressRequests.cs` | `ProjectLink.Contracts.Progress` | Batch stage-progress query |
| `Progress/ProgressResponses.cs` | `ProjectLink.Contracts.Progress` | Stage progress list with unlock status |
| `Daily/DailyChallengeRequests.cs` | `ProjectLink.Contracts.Daily` | Daily challenge complete trigger |
| `Daily/DailyChallengeResponses.cs` | `ProjectLink.Contracts.Daily` | Challenge state, streak tiles, rewards |
| `Reward/RewardRequests.cs` | `ProjectLink.Contracts.Reward` | Generic reward claim (ad / daily login) |
| `Reward/RewardResponses.cs` | `ProjectLink.Contracts.Reward` | Granted rewards + balance deltas |
| `Shop/ShopRequests.cs` | `ProjectLink.Contracts.Shop` | Product purchase request |
| `Shop/ShopResponses.cs` | `ProjectLink.Contracts.Shop` | Catalog listing and purchase result |
| `Event/SeasonEventResponses.cs` | `ProjectLink.Contracts.Event` | Active season event list |
| `Settings/PlayerSettingsRequests.cs` | `ProjectLink.Contracts.Settings` | PATCH player preferences (all fields nullable) |
| `Settings/PlayerSettingsResponses.cs` | `ProjectLink.Contracts.Settings` | Full player settings state |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `ErrorResponse` | class | `ErrorCode` (string) — client maps code to localized message via error_messages.csv |
| `BootstrapConfigResponse` | class | `ClientVersion`, `RequiredClientVersion`, `ProtocolVersion`, `MetaHash`, `ServerTimeUtc`, `Maintenance`, `MaintenanceMessage?` |
| `GuestLoginRequest` | class | empty body |
| `SocialLoginRequest` | class | `Provider` ("google"\|"apple"), `IdToken` |
| `RefreshTokenRequest` | class | `RefreshToken` |
| `LinkAccountRequest` | class | `Provider`, `IdToken` |
| `AuthResponse` | class | `AccessToken`, `RefreshToken`, `ExpiresAt`, `Profile` (AccountMeResponse) |
| `AccountMeResponse` | class | `UserId`, `DisplayName`, `IsGuest`, `LinkedProviders`, `AvatarId`, `CreatedAt` |
| `StageStartRequest` | class | empty body — stageId in route |
| `StageStartResponse` | class | `SessionToken`, `ServerStartAt`, `MoveLimit` (0=unlimited), `TimeLimitSeconds` (0=unlimited), `ItemCounts` (Dict\<int,int\>), `StaminaCurrent` |
| `StageEndRequest` | class | `SessionToken`, `Result` ("success"\|"fail"), `ClientElapsedMs`, `MovesUsed` |
| `StageEndResponse` | class | `Score`, `Stars`, `AdjustedElapsedMs`, `IsBestRecord`, `SoftBalanceAfter`, `SoftReward`, `MovesUsed`, `MoveLimit`, `NextStageId?`, `NextStageUnlocked` |
| `StaminaAdRewardRequest` | class | `AdToken` — platform-issued, idempotency key |
| `StaminaRefillRequest` | class | empty — server reads cost from static data |
| `StaminaResponse` | class | `Current`, `Max`, `NextRechargeAt?` (ISO 8601) |
| `StaminaAdRewardResponse` | class | `Current`, `Max`, `Added`, `NextRechargeAt?` |
| `StaminaRefillResponse` | class | `Current`, `Max`, `Added`, `SoftCost`, `SoftBalanceAfter`, `NextRechargeAt?` |
| `CurrencyAdRewardRequest` | class | `AdToken` — idempotency key |
| `CurrencyResponse` | class | `SoftAmount` |
| `CurrencyAdRewardResponse` | class | `SoftAmountAfter`, `Added` |
| `ItemPurchaseRequest` | class | `ItemId`, `Quantity` |
| `ItemUseEntry` | class | single item in a use batch: `ItemId`, `Quantity` |
| `ItemUseRequest` | class | `StageSessionToken`, `Items` (List\<ItemUseEntry\>) |
| `InventorySlot` | class | `ItemId`, `Quantity` — shared by inventory and use responses |
| `InventoryResponse` | class | `Items` (List\<InventorySlot\>) — quantity > 0 only |
| `ItemPurchaseResponse` | class | `ItemId`, `QuantityAfter`, `SoftBalanceAfter` |
| `ItemUseResponse` | class | `UpdatedSlots` (List\<InventorySlot\>) — changed entries only |
| `RankingEntry` | class | `Rank`, `UserId`, `DisplayName`, `Value`, `AvatarId`, `IsMe` |
| `RankingListResponse` | class | `Entries`, `MyRank?`, `Category`, `MetricLabel`, `NextCursor?` |
| `MyRankEntry` | class | `Rank`, `Value` — one category |
| `MyRankResponse` | class | `StagesCleared?`, `TotalScore?` — `/api/ranking/me` |
| `LobbyStateResponse` | class | `Profile`, `Stamina`, `Currency`, `ProgressSummary`, `DailyChallenge`, `SeasonEvent?` |
| `LobbyProfile` | class | `DisplayName`, `AvatarId` |
| `LobbyStamina` | class | `Current`, `Max`, `NextRechargeAt?` |
| `LobbyCurrency` | class | `SoftAmount` |
| `LobbyProgressSummary` | class | `HighestStageId`, `TotalStarsEarned`, `NextUnlockedStageId` |
| `LobbyDailyChallenge` | class | `CompletedToday`, `CanComplete`, `PlayCountToday`, `PlayCountTarget`, `StreakDays`, `ResetAt` |
| `LobbySeasonEvent` | class | `EventId`, `Name`, `EndAt`, `IsActive` — nullable; omitted if no active event |
| `BatchProgressRequest` | class | `StageIds` (List\<int\>) — query, not upsert |
| `StageProgressEntry` | class | `StageId`, `Stars`, `IsUnlocked`, `ClearedAt?` |
| `ProgressResponse` | class | `Stages` (List\<StageProgressEntry\>) |
| `DailyChallengeCompleteRequest` | class | empty body |
| `DailyChallengeResponse` | class | `TodayStageIds` (List\<int\>, date-seeded), `CompletedToday`, `CanComplete`, `PlayCountToday`, `PlayCountTarget`, `StreakDays`, `ResetAt`, `Tiles`, `TodayRewards` |
| `DailyChallengeStreakTile` | class | `Day`, `IsDone`, `IsToday`, `IsLocked` |
| `DailyChallengeRewardPreview` | class | `RewardType`, `RewardId`, `Amount` — preview before completion |
| `DailyChallengeCompleteResponse` | class | `RewardsGranted`, `StreakDays`, `SoftBalanceAfter`, `InventoryUpdates` |
| `DailyInventoryUpdate` | class | `ItemId`, `QuantityAfter` |
| `RewardClaimRequest` | class | `RewardSource`, `RewardToken`, `Multiplier` |
| `RewardClaimResponse` | class | `RewardsGranted`, `SoftBalanceAfter`, `InventoryUpdates` |
| `ShopPurchaseRequest` | class | `ProductId`, `Quantity` (default 1), `IapReceiptData?` (null for soft-currency) |
| `ShopCatalogResponse` | class | `Products` (List\<ShopProductEntry\>), `SoftBalance` |
| `ShopProductEntry` | class | `ProductId`, `Category`, `Name`, `GrantItemId`, `GrantQuantity`, `PriceSoft`, `PriceIapSku?`, `SortOrder` |
| `ShopPurchaseResponse` | class | `ProductId`, `SoftBalanceAfter`, `InventoryUpdates` |
| `SeasonEventEntry` | class | `EventId`, `Name`, `Type`, `StartAt`, `EndAt`, `MetricLabel`, `IsActive`, `IsLocked` |
| `ActiveEventsResponse` | class | `Events` (List\<SeasonEventEntry\>) |
| `PlayerSettingsUpdateRequest` | class | all fields nullable — PATCH semantics, only non-null fields applied |
| `PlayerSettingsResponse` | class | `BgmEnabled`, `SfxEnabled`, `HapticsEnabled`, `NotificationsEnabled`, `Language` |

## Cross-refs
- Consumed by: server `API.Controllers.*` (all controllers — request/response bodies)
- Consumed by: server `Application.*Service` (return types)
- Consumed by: client `Generated/Contracts/` (Unity copy of these .cs files — manual sync)
