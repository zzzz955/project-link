# ProjectLink.Application/Stamina

## Files
| file | class | role |
|------|-------|------|
| `StaminaService.cs` | `StaminaService` | Stamina read, ad-reward grant, and soft-currency refill |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `StaminaService.GetAsync` | method | Returns current stamina state with lazy-recharge applied |
| `StaminaService.AdRewardAsync` | method | Grants stamina via ad token; deduplicates via `IAdTokenRepository` |
| `StaminaService.RefillAsync` | method | Full refill using soft currency; delegates to `IStaminaRefillTransaction`; throws `StaminaAlreadyFullException` or `InsufficientFundsException` |

## Cross-refs
- Depends on: `IStaminaRepository`, `IAdTokenRepository`, `IStaminaRefillTransaction`, `IStaticDataService`
- Consumed by: `API.Controllers.StaminaController` → `GET /api/stamina`, `POST /api/stamina/ad-reward`, `POST /api/stamina/refill`

## Rules
- Refill cost is authoritative from `IStaticDataService.GetStaminaConfig().RefillCostSoft` — never trust client-provided cost
- `RefillAsync` is atomic: stamina increment + currency deduction in `IStaminaRefillTransaction`
