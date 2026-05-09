# Services - Client data/service boundaries

## Files
| file | class | role |
|---|---|---|
| `IUiDataService.cs` | `IUiDataService` | UI-facing async service contract for server-backed screen state and mutations |
| `HttpUiDataService.cs` | `HttpUiDataService` | MonoBehaviour HTTP adapter + short TTL cache implementing `IUiDataService` through `NetworkManager` |
| `UiServiceLocator.cs` | `UiServiceLocator` | Runtime UI service/catalog resolver for scene and popup controllers |
| `UiDataRoutes.cs` | `UiDataRoutes` | Stable API route constants used by future HTTP adapters |
| `StaticCatalogService.cs` | `IStaticCatalogService`, `StaticCatalogService` | Planning-table access facade for outgame/static UI display data |
| `UiViewModels.cs` | `LobbyScreenModel`, `ShopScreenModel`, `EnergyPopupModel` | UI-consumable models built from server DTOs + generated CSV metadata |
| `UiViewModelMapper.cs` | `UiViewModelMapper` | Maps server DTOs into UI models without hardcoded display data |

## Symbols
| symbol | kind | note |
|---|---|---|
| `ServiceResult<T>` | struct | success/error wrapper for UI service callbacks |
| `IUiDataService.GetBootstrapConfig(...)` | method | returns bootstrap/version config from server |
| `IUiDataService.GetAccountMe(...)` | method | returns account/profile server state |
| `IUiDataService.GetLobbyState(...)` | method | returns aggregate lobby server state |
| `IUiDataService.GetShopCatalog(...)` | method | returns server shop catalog and balance |
| `IUiDataService.GetRanking(...)` | method | returns server leaderboard state |
| `IUiDataService.GetDailyChallenge(...)` | method | returns server daily challenge state |
| `IUiDataService.StartStage(...)` | method | returns server stage session state |
| `IUiDataService.EndStage(...)` | method | submits stage result and returns clear-popup reward/progress fields |
| `IUiDataService.ClaimStaminaAdReward(...)` | method | calls stamina ad reward route and returns current/max/added data |
| `IUiDataService.RefillStamina(...)` | method | calls paid stamina refill route and returns current/max/added/cost data |
| `IUiDataService.PurchaseShopProduct(...)` | method | calls shop purchase route and returns balance/inventory updates |
| `HttpUiDataService.GetLobbyState(...)` | method | GET adapter for lobby state |
| `HttpUiDataService.InvalidateStageCaches()` | method | clears cached lobby/progress/daily/stamina after stage mutations |
| `HttpUiDataService.ClaimReward(...)` | method | POST adapter for reward claim |
| `UiServiceLocator.UiData` | prop | resolves or creates the `IUiDataService` adapter used by UI controllers |
| `UiServiceLocator.Catalog` | prop | shared planning-table catalog facade |
| `UiDataRoutes.StageStart(int)` | method | builds stage start API route |
| `UiDataRoutes.Ranking(string)` | method | maps UI ranking segment to stable server endpoint |
| `StaticCatalogService.GetEnabledShopProducts(string)` | method | reads enabled shop products from generated outgame CSV |
| `StaticCatalogService.FindItem(int)` | method | resolves item display metadata from generated ingame CSV |
| `UiViewModelMapper.ToLobbyScreen(...)` | method | maps `LobbyStateResponse` + static avatar/event metadata |
| `UiViewModelMapper.ToShopScreen(...)` | method | maps `ShopCatalogResponse` + item metadata |
| `UiViewModelMapper.ToEnergyPopup(...)` | method | maps `StaminaResponse` + stamina config metadata |

## Cross-refs
- Consumed by: future UI controllers generated/bound from `Editor.ProjectLinkUIBuilder`
- Depends on: client `Data.OutgameDataLoader`, client `Generated/Contracts`

## Rules
- UI controllers depend on these interfaces, not on server controller classes.
- Real display text/state must come from server DTOs, generated CSV data, or localized string IDs.
- Do not add dummy/mock values here; temporary UI mocks belong in test fixtures or explicit debug adapters.
