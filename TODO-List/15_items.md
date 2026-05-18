# 15 — Item System

## Spec
| key | value |
|-----|-------|
| Types | ObstacleRemove, NodePairRemove, MoveReducer, TimeExtender |
| Usage timing | During active in-game play (ingame items) |
| Purchase | Soft currency |

### Item Types
| id | type | effect |
|----|------|--------|
| 1 | `OBSTACLE_REMOVE` | Highlights obstacle cells; tap removes obstacle → walkable cell (client-only board mutation) |
| 2 | `NODE_PAIR_REMOVE` | Highlights all nodes; tap removes node pair → group no longer required for clear (client-only board mutation) |
| 3 | `MOVE_REDUCE` | `movesUsed -= 3`; unusable when `movesUsed < 3` |
| 4 | `TIME_EXTEND` | Adds 20 s to remaining timer |

Board state changes (obstacle/node removal) are client-side only. Server validates item ownership and deducts inventory via `POST /api/items/use-ingame`.

---

## Static Data (CSV)

File: `shared/datas/ingame/ingame_item.csv`

| column | type | description |
|--------|------|-------------|
| id | int | unique item id |
| name | string | display name |
| type | string | `OBSTACLE_REMOVE` \| `NODE_PAIR_REMOVE` \| `MOVE_REDUCE` \| `TIME_EXTEND` |
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
| POST | `/api/items/use` | JWT | Use item(s) before stage start (setup-phase only) |
| POST | `/api/items/use-ingame` | JWT | Use item during active in-game play; validates session token, deducts inventory |

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

### POST /api/items/use-ingame
```json
{ "stageSessionToken": "...", "itemId": 1 }
```
- Does NOT check `IsSetupPhase` (client never calls `/lock`, sessions remain in setup state throughout)
- Validates session token matches active session
- Deducts 1 quantity; returns `{ itemId, quantityAfter }`
- Board effect applied client-side only after success response

---

## Stage Flow Integration

```
POST /api/stage/{id}/start
  → creates session in Redis with state: { items_used: [] }
  → returns ItemCounts (all item types with quantity > 0)

[client displays item toolbar with quantities]

POST /api/items/use-ingame  (during play, per-use)
  → validates session token
  → deducts 1 inventory
  → returns { itemId, quantityAfter }
  → client applies board effect and updates HUD

POST /api/stage/{id}/end
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
- [x] Finalize 4 ingame item specs: OBSTACLE_REMOVE, NODE_PAIR_REMOVE, MOVE_REDUCE, TIME_EXTEND
- [x] Replace mockup rows in `ingame_item.csv`; sync client/server generated CSVs
- [x] Add `InGameItemUseRequest` / `InGameItemUseResponse` to shared contracts
- [x] `POST /api/items/use-ingame` controller + `InventoryService.UseIngameItemAsync`
- [x] `StageService.StartAsync` — return all item types in `ItemCounts` (not filtered by POWER_UP)
- [x] `GameContext.ItemCounts` — stores item quantities from stage start response
- [x] `GameStateMachine` — add Idle→Completed transition for node-pair eraser
- [x] `Cell.SetEmpty()` — clear node/obstacle in-place
- [x] `Board.RemoveObstacle` / `Board.RemoveNodePair` — in-game board mutations
- [x] `BoardView.SetHighlights` / `ClearHighlights` — item selection highlight mode
- [x] `CellView.SetHighlight` / `ClearHighlight` — per-cell highlight with color override
- [x] `PathDrawer.RemoveGroupPaths` / `CheckCleared` — post-erase path cleanup + win check
- [x] `TouchInputHandler.OnTap` — separate tap event for item selection mode
- [x] `InGameHUD.InitItemToolbar` / `SetItemButtonState` / `UpdateItemCount` / `SetTotalColors`
- [x] `GameWireframeController` — item button/count text Inspector refs
- [x] `InGameController` — full item use flow (all 4 items, selection mode, server call)
- [x] `IUiDataService.UseIngameItem` + `HttpUiDataService` + `UiDataRoutes.UseIngameItem`

<!-- changed: items confirmed — 4 ingame items (OBSTACLE_REMOVE, NODE_PAIR_REMOVE, MOVE_REDUCE, TIME_EXTEND); usage during gameplay not pre-stage; full client+server implementation complete -->
