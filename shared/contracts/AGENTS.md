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
| `Stage/StageRequests.cs` | `ProjectLink.Contracts.Stage` | Stage start/end request bodies |
| `Stage/StageResponses.cs` | `ProjectLink.Contracts.Stage` | Stage start/end response shapes |
| `Stamina/StaminaRequests.cs` | `ProjectLink.Contracts.Stamina` | Ad-reward and extend request bodies |
| `Stamina/StaminaResponses.cs` | `ProjectLink.Contracts.Stamina` | Stamina state and reward responses |
| `Currency/CurrencyRequests.cs` | `ProjectLink.Contracts.Currency` | Ad-reward request body |
| `Currency/CurrencyResponses.cs` | `ProjectLink.Contracts.Currency` | Balance and reward responses |
| `Item/ItemRequests.cs` | `ProjectLink.Contracts.Item` | Purchase and use request bodies |
| `Item/ItemResponses.cs` | `ProjectLink.Contracts.Item` | Inventory and purchase responses |
| `Ranking/RankingResponses.cs` | `ProjectLink.Contracts.Ranking` | Ranking list and my-rank responses |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `ErrorResponse` | class | `ErrorCode` (string) — client maps code to localized message via error_messages.csv |
| `StageStartRequest` | class | empty body — stageId in route |
| `StageStartResponse` | class | `SessionToken`, `ServerStartAt` (ISO 8601) |
| `StageEndRequest` | class | `Result` ("success"\|"fail"), `ClientElapsedMs` |
| `StageEndResponse` | class | `Score`, `Stars`, `AdjustedElapsedMs`, `IsBestRecord`, `SoftBalanceAfter` |
| `StaminaAdRewardRequest` | class | `AdToken` — platform-issued, idempotency key |
| `StaminaExtendRequest` | class | empty — server reads cost from config |
| `StaminaResponse` | class | `Current`, `Max`, `NextRechargeAt?` (ISO 8601) |
| `StaminaAdRewardResponse` | class | `Current`, `Added`, `NextRechargeAt?` |
| `StaminaExtendResponse` | class | `StaminaCurrent`, `SoftBalanceAfter` |
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
| `RankingEntry` | class | `Rank`, `UserId`, `DisplayName`, `Value` |
| `RankingListResponse` | class | `Entries`, `MyRank?` — used for all 3 list endpoints |
| `MyRankEntry` | class | `Rank`, `Value` — one category |
| `MyRankResponse` | class | `StagesCleared?`, `TotalScore?` — `/api/ranking/me` |
