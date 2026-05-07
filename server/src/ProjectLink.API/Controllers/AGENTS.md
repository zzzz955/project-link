# ProjectLink.API/Controllers

## Files
| file | class | route prefix | auth |
|------|-------|--------------|------|
| `HealthController.cs` | `HealthController` | `GET /health` | none |
| `BootstrapController.cs` | `BootstrapController` | `/api/bootstrap` | none |
| `StageController.cs` | `StageController` | `/api/stage` | JWT |
| `StaminaController.cs` | `StaminaController` | `/api/stamina` | JWT |
| `CurrencyController.cs` | `CurrencyController` | `/api/currency` | JWT |
| `InventoryController.cs` | `InventoryController` | `/api/inventory` | JWT |
| `RankingController.cs` | `RankingController` | `/api/ranking` | JWT |
| `LobbyController.cs` | `LobbyController` | `/api/lobby` | JWT |
| `ProgressController.cs` | `ProgressController` | `/api/progress` | JWT |
| `DailyChallengeController.cs` | `DailyChallengeController` | `/api/daily-challenge` | JWT |
| `ShopController.cs` | `ShopController` | `/api/shop` | JWT |
| `PlayerSettingsController.cs` | `PlayerSettingsController` | `/api/settings` | JWT |
| `EventController.cs` | `EventController` | `/api/events` | none |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `BootstrapController.GetConfig` | method | `GET /api/bootstrap/config` — no auth; returns `BootstrapConfigResponse` |
| `StageController.Start` | method | `POST /api/stage/{stageId}/start` |
| `StageController.Lock` | method | `POST /api/stage/{stageId}/lock` — body: `{ sessionToken }` |
| `StageController.End` | method | `POST /api/stage/{stageId}/end` — body: `StageEndRequest` |
| `StageController.Extend` | method | `POST /api/stage/{stageId}/extend` — body: `{ sessionToken }` |
| `StaminaController.Get` | method | `GET /api/stamina` |
| `StaminaController.AdReward` | method | `POST /api/stamina/ad-reward` |
| `StaminaController.Refill` | method | `POST /api/stamina/refill` |
| `RankingController.GetList` | method | `GET /api/ranking/{category}` — category: `stages_cleared`\|`total_score` |
| `RankingController.GetMe` | method | `GET /api/ranking/me` |
| `LobbyController.Get` | method | `GET /api/lobby` — returns full `LobbyStateResponse` |
| `ProgressController.BatchGet` | method | `POST /api/progress/batch` — query by stageIds (not upsert) |
| `DailyChallengeController.Get` | method | `GET /api/daily-challenge` |
| `DailyChallengeController.Complete` | method | `POST /api/daily-challenge/complete` |
| `ShopController.GetCatalog` | method | `GET /api/shop/catalog` |
| `ShopController.Purchase` | method | `POST /api/shop/purchase` |
| `PlayerSettingsController.Get` | method | `GET /api/settings` |
| `PlayerSettingsController.Update` | method | `PATCH /api/settings` — partial update |
| `EventController.GetSeasonEvents` | method | `GET /api/events/season` — no auth |

## Cross-refs
- Depends on: all `Application.*Service` and `Application.*Query/Command` types
- Consumed by: client HTTP adapters

## Rules
- `userId` extracted from JWT claim `sub` in every authenticated controller — never from request body
- `ProgressController.BatchGet` is a query (POST with body for stageIds), not an upsert
- `EventController` and `BootstrapController` are unauthenticated public endpoints
