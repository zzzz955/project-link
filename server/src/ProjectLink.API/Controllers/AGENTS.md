# ProjectLink.API/Controllers

## Files
| file | class | route prefix | auth |
|------|-------|--------------|------|
| `HealthController.cs` | `HealthController` | `GET /health` | none |
| `BootstrapController.cs` | `BootstrapController` | `/api/bootstrap` | none |
| `AuthController.cs` | `AuthController` | `/api/auth` | none |
| `AccountController.cs` | `AccountController` | `/api/account` | JWT |
| `StageController.cs` | `StageController` | `/api/stage` | JWT |
| `StaminaController.cs` | `StaminaController` | `/api/stamina` | JWT |
| `CurrencyController.cs` | `CurrencyController` | `/api/currency` | JWT |
| `InventoryController.cs` | `InventoryController` | `/api/inventory`, `/api/items` | JWT |
| `RankingController.cs` | `RankingController` | `/api/ranking` | JWT |
| `LobbyController.cs` | `LobbyController` | `/api/lobby` | JWT |
| `ProgressController.cs` | `ProgressController` | `/api/progress` | JWT |
| `StreakChallengeController.cs` | `StreakChallengeController` | `/api/streak-challenge` | JWT |
| `ShopController.cs` | `ShopController` | `/api/shop` | JWT |
| `RewardController.cs` | `RewardController` | `/api/rewards` | JWT |
| `PlayerSettingsController.cs` | `PlayerSettingsController` | `/api/settings` | JWT |
| `EventController.cs` | `EventController` | `/api/events` | none |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `BootstrapController.GetConfig` | method | `GET /api/bootstrap/config`; no auth; returns `BootstrapConfigResponse` |
| `AuthController.Guest` | method | `POST /api/auth/guest`; mock: returns `AuthResponse` with mock:guest token; real: proxies to platform, transforms `AuthSessionResponse` → `AuthResponse` |
| `AuthController.Refresh` | method | `POST /api/auth/refresh`; mock: returns fresh mock `AuthResponse`; real: proxies to `{authority}/auth/refresh`, transforms response |
| `AuthController.Google` | method | `POST /api/auth/google`; mock: 400 PROVIDER_UNAVAILABLE; real: proxies to `{authority}/auth/google`, transforms response |
| `AuthController.Logout` | method | `POST /api/auth/logout`; mock: 204; real: proxies to `{authority}/auth/logout` |
| `AccountController.Me` | method | `GET /api/account/me`; returns authenticated `AccountMeResponse` |
| `StageController.Start` | method | `POST /api/stage/{stageId}/start` |
| `StageController.Lock` | method | `POST /api/stage/{stageId}/lock`; body includes `sessionToken` |
| `StageController.End` | method | `POST /api/stage/{stageId}/end`; body: `StageEndRequest` |
| `StageController.Extend` | method | `POST /api/stage/{stageId}/extend`; body includes `sessionToken` |
| `StaminaController.Get` | method | `GET /api/stamina` |
| `StaminaController.AdReward` | method | `POST /api/stamina/ad-reward` |
| `StaminaController.Refill` | method | `POST /api/stamina/refill` |
| `InventoryController.Get` | method | `GET /api/inventory` |
| `InventoryController.Purchase` | method | `POST /api/items/purchase` |
| `InventoryController.Use` | method | `POST /api/items/use`; validates `StageSessionToken` and records item usage |
| `InventoryController.UseIngame` | method | `POST /api/items/use-ingame`; validates session token, deducts item quantity; does NOT check `IsSetupPhase` (client never calls lock); returns `InGameItemUseResponse` |
| `RankingController.GetStage` | method | `GET /api/ranking/stage/{stageId}` |
| `RankingController.GetGlobalStages` | method | `GET /api/ranking/global/stages` |
| `RankingController.GetGlobalScore` | method | `GET /api/ranking/global/score` |
| `RankingController.GetMe` | method | `GET /api/ranking/me` |
| `LobbyController.Get` | method | `GET /api/lobby`; returns `LobbyStateResponse` |
| `ProgressController.Get` | method | `GET /api/progress`; returns all progress entries |
| `ProgressController.GetBatch` | method | `POST /api/progress/batch`; query by stageIds |
| `StreakChallengeController.Get` | method | `GET /api/streak-challenge` |
| `StreakChallengeController.Activate` | method | `POST /api/streak-challenge/activate` |
| `StreakChallengeController.StartLevel` | method | `POST /api/streak-challenge/level/{level}/start` |
| `StreakChallengeController.ClaimReward` | method | `POST /api/streak-challenge/level/{level}/claim-reward` |
| `StreakChallengeController.ClaimRewardWithAd` | method | `POST /api/streak-challenge/level/{level}/claim-reward-with-ad` |
| `ShopController.GetCatalog` | method | `GET /api/shop/catalog` |
| `ShopController.Purchase` | method | `POST /api/shop/purchase` |
| `RewardController.Claim` | method | `POST /api/rewards/claim`; returns `RewardClaimResponse` |
| `PlayerSettingsController.Get` | method | `GET /api/settings` |
| `PlayerSettingsController.Update` | method | `PATCH /api/settings`; partial update |
| `EventController.GetSeasonEvents` | method | `GET /api/events/season`; no auth |

## Cross-refs
- Depends on: all `Application.*Service` and `Application.*Query/Command` types.
- Consumed by: client HTTP adapters.

## Rules
- `userId` extracted from JWT claim `sub` in every authenticated controller; never from request body.
- `ProgressController.GetBatch` is a query (POST with body for stageIds), not an upsert.
- `EventController` and `BootstrapController` are unauthenticated public endpoints.
