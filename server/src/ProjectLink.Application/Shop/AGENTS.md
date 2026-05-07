# ProjectLink.Application/Shop

## Files
| file | class | role |
|------|-------|------|
| `ShopService.cs` | `ShopService` | Shop catalog read and soft-currency purchase |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `ShopService.GetCatalogAsync` | method | Returns enabled products ordered by sortOrder + current balance |
| `ShopService.PurchaseAsync` | method | Validates category=ITEM, quantity>0, then calls `IShopPurchaseTransaction` |

## Cross-refs
- Consumed by: `API.Controllers.ShopController` → `GET /api/shop/catalog`, `POST /api/shop/purchase`
- Depends on: `IStaticDataService`, `ICurrencyRepository`, `IShopPurchaseTransaction`

## Rules
- Only ITEM category is purchasable at launch (IAP categories return ShopItemNotFoundException)
- Authoritative price from `OutgameShopCatalogData.PriceSoft` — client-provided price is not trusted
- `totalCost = product.PriceSoft * quantity` — quantity > 1 multiplies cost
