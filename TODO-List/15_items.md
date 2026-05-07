# 15 — Item System

## Spec
| key | value |
|-----|-------|
| Types | ObstacleRemove, NodePairRemove |
| Usage timing | Before stage start |
| Purchase | Soft currency |

### Item Types
| type | effect |
|------|--------|
| `OBSTACLE_REMOVE` | Removes a specific obstacle at a given board position |
| `NODE_PAIR_REMOVE` | Removes a specific pre-placed node pair |

Both are applied client-side before the stage begins. Server validates usage and deducts inventory.

---

## Static Data (CSV)

File: `shared/datas/ingame/ingame_item.csv`

| column | type | description |
|--------|------|-------------|
| id | int | unique item id |
| name | string | display name |
| type | string | `OBSTACLE_REMOVE` \| `NODE_PAIR_REMOVE` |
| cost_soft | int | soft currency purchase price |
| description | string | UI tooltip |

Items are design-time constants. Adding a new item = new CSV row + re-run `npm run gen:data`.

---

## DB

### Table: `inventory`
| column | type | constraints |
|--------|------|-------------|
| user_id | string(36) | PK NN |
| item_id | int32 | PK NN |
| quantity | int32 | NN |

Composite PK `(user_id, item_id)`. `quantity` never goes negative (enforced by `WHERE quantity >= @count`).

---

## API Endpoints

| method | path | auth | description |
|--------|------|------|-------------|
| GET | `/api/inventory` | JWT | Returns all items with quantity > 0 |
| POST | `/api/items/purchase` | JWT | Buy item with soft currency |
| POST | `/api/items/use` | JWT | Use item(s) before stage start |

### POST /api/items/purchase
```json
{ "item_id": 1, "quantity": 1 }
```
- Atomic: deduct currency + grant inventory in one transaction
- Response: updated inventory entry

### POST /api/items/use
```json
{ "stage_session_token": "...", "items": [{ "item_id": 1, "quantity": 1 }] }
```
- Validates: stage session is active (Redis), stage not yet started (in setup phase)
- Atomic: deduct inventory quantities + record usage on session
- Items applied to session metadata → client reads session state at start

---

## Stage Flow Integration

```
POST /api/stage/{id}/start
  → creates session in Redis with state: { items_used: [] }

POST /api/items/use
  → validates session exists and is in "setup" phase
  → deducts inventory
  → appends to session.items_used

[client starts stage with items]

POST /api/stage/{id}/end
  → reads session.items_used for validation / logging
  → closes session
```

---

## Tasks

- [x] Create `shared/datas/ingame/ingame_item.csv` with initial item definitions
- [x] Run `npm run gen:data` to distribute CSV
- [x] Add `inventory` table to `server/db/schema.json` → run `npm run gen:orm`
- [x] `InventoryService.Deduct(userId, itemId, quantity)` — atomic WHERE quantity >= n
- [x] `InventoryService.Grant(userId, itemId, quantity)` — UPSERT
- [x] `GET /api/inventory` controller
- [x] `POST /api/items/purchase` controller (currency deduct + inventory grant)
- [x] `POST /api/items/use` controller (session token + phase validation + inventory deduct)
- [x] Integrate item usage into stage session model
- [x] `ItemRequests.cs` / `ItemResponses.cs` in `shared/contracts/Item/`

<!-- changed: items defined — ObstacleRemove + NodePairRemove, pre-stage usage, soft currency purchase -->
