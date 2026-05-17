# shared/datas/outgame — Out-game Configuration Data

## Tables
| file | rows | key |
|------|------|-----|
| `outgame_stamina_config.csv` | Global stamina config (1 row) | `configId` (PK) |
| `outgame_avatar.csv` | Preset avatar icons | `id` (PK) |
| `outgame_shop_catalog.csv` | Shop product catalog | `productId` (PK) |
| `outgame_season_event.csv` | Season event definitions | `eventId` (PK) |
| `outgame_daily_reward.csv` | 7-day daily login reward table | `streakDay` (PK) |
| `outgame_time_extend_config.csv` | Time extension config (cost/seconds per extension count) | `extensionCount` (PK) |

## Schema

**outgame_stamina_config**
- `configId` int32 PK — always 1 (single global config)
- `maxStamina` int32 NN — max stamina cap
- `rechargeSeconds` int32 NN — seconds to recover 1 stamina
- `refillCostSoft` int32 NN — soft currency cost for full refill (server-only)
- `adRewardAmount` int32 NN — stamina restored by watching an ad

**outgame_avatar**
- `id` int32 PK
- `name` string(64) NN — display name
- `iconPath` string(128) NN — relative resource path
- `unlockCondition` string(64) NN — `default` = always available

**outgame_shop_catalog**
- `productId` int32 PK
- `category` string(32) NN — `ITEM` | `COIN` | `BUNDLE` | `NO_ADS`
- `name` string(64) NN — display name
- `grantItemId` int32 NN — `ingame_item.id` for ITEM; 0 for other categories
- `grantQuantity` int32 NN — quantity granted (soft amount for COIN)
- `priceSoft` int32 NN — soft currency cost; 0 for IAP products
- `priceIapSku` string(128) — store SKU; empty for soft-only products
- `sortOrder` int32 NN — ascending display order
- `isEnabled` bool NN — false = hidden from catalog

**outgame_season_event**
- `eventId` int32 PK
- `name` string(64) NN — display name
- `type` string(32) NN — `COLOR_CUP`
- `startAt` string(32) NN — ISO 8601 UTC
- `endAt` string(32) NN — ISO 8601 UTC
- `metricLabel` string(64) NN — ranking metric label shown in UI
- `rankingMetric` string(32) NN — server ranking key (server-only)

**outgame_daily_reward**
- `streakDay` int32 PK
- `rewardType` string(32) NN
- `rewardId` int32 NN
- `amount` int32 NN

**outgame_time_extend_config**
- `extensionCount` int32 PK — 1-based (1st extension, 2nd extension, …)
- `extendSeconds` int32 NN — seconds added to client timer
- `costSoft` int32 NN — soft currency deducted on server

## Cross-refs
- Gen output: `client/generated/data/outgame/` and `server/generated/data/outgame/`
- Consumed by: server `Infrastructure.Data.StaticDataService`
- Consumed by: server `Domain.StaticData.OutgameStaminaConfigData`, `OutgameAvatarData`, `OutgameShopCatalogData`, `OutgameSeasonEventData`
- Consumed by: client `OutGame.UI.DailyRewardPopup`

## Rules
- `outgame_stamina_config` always has exactly 1 row (configId=1)
- ITEM shop products: `grantItemId` must reference a valid `ingame_item.id`
- IAP products (`COIN`, `BUNDLE`, `NO_ADS`): `priceSoft=0`, `priceIapSku` must be non-empty
- Soft products (`ITEM`): `priceIapSku` is empty, `priceSoft > 0`
- Daily rewards must define days 1 through 7 exactly once.
