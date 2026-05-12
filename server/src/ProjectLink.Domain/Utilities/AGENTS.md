# ProjectLink.Domain/Utilities

## Files
| file | class | role |
|------|-------|------|
| `IdHelper.cs` | `IdHelper` | Generates 16-digit random long IDs safe for JSON number precision |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `IdHelper.NewId` | static method | Returns `Random.Shared.NextInt64(10^15, 2^53).ToString()`; safe for JavaScript/JSON (≤ 2^53); replaces Guid usage for all transaction/session IDs |

## Cross-refs
- Consumed by: `Application.Currency.CurrencyService`, `Application.Stage.StageService`, `Application.Session.SessionService`, `Application.Reward.RewardService`, `Application.Inventory.InventoryService`, `Infrastructure.Persistence.StageEndTransactionRepository`, `Infrastructure.Persistence.StaminaRefillTransactionRepository`, `Infrastructure.Persistence.ShopPurchaseTransactionRepository`, `Infrastructure.Persistence.DailyChallengeCompleteTransactionRepository`

## Rules
- Range `[10^15, 2^53)` guarantees exactly 16 digits and JSON number safety
- Do not use `Guid.NewGuid()` for any new transaction/session/token IDs — use `IdHelper.NewId()`
