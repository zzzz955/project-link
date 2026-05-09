# 17 - UI Wireframe Server Contracts

Goal: update all `ProjectLinkUIBuilder` generated UI to match `Color Paths - Unity6 Wireframes`, but do not bind the new UI to HTTP until the server DTO/API contract is stable.

Reference:
- `C:/Users/SangHyeok/Downloads/Color Paths * Unity Wireframes.pdf`
- Client builder: `client/project-link/Assets/Scripts/Editor/ProjectLinkUIBuilder.cs`
- Shared DTO source: `shared/contracts/`
- Server API: `server/src/ProjectLink.API/Controllers/`

---

## Direction

- [x] Treat the PDF as the UI source of truth for generated scene/popup hierarchy.
- [x] Keep UI work service-interface driven; real labels/states must come from server DTOs or generated CSV metadata.
- [x] Server agents should define DTOs in `shared/contracts/` first, then implement controllers.
- [x] UI agents should bind only to service interfaces, generated CSV metadata, and stable server DTOs.
- [ ] Detailed server business rules are delegated to the server implementation agent.

---

## Contract Gate Before New UI Binding

The new UI can be generated visually before this gate, but real server binding starts only after:

- [x] `Auth`, `Player/Lobby`, `Progress`, `Stage`, `Stamina`, `Currency`, `Item`, `Ranking`, `Daily`, `Reward`, `Settings` DTOs needed below are present or explicitly deferred.
- [x] Controller routes are named and stable.
- [x] Each response contains enough data for PDF labels, counters, disabled states, badges, timers, and popups.
- [x] Error responses remain unified through `Common/ErrorResponse`.
- [x] Unity-side HTTP adapters use mock guest auth and stable DTOs; local test fallback remains explicit.

---

## Existing Coverage

| Domain | Existing | Gap |
|---|---|---|
| Stamina | `GET /api/stamina`, `POST /api/stamina/ad-reward`, `POST /api/stamina/extend` | Refill popup needs explicit reward/cost/max/current data aligned to UI |
| Currency | `GET /api/currency`, `POST /api/currency/ad-reward` | Lobby/reward popup needs grant result DTO consistency |
| Inventory/Items/Shop | `GET /api/inventory`, `POST /api/items/purchase`, `POST /api/items/use`, `GET /api/shop/catalog`, `POST /api/shop/purchase` | BuyItem/Shop UI should prefer server catalog and merge generated CSV item metadata |
| Stage | `POST /api/stage/{id}/start|lock|end|extend` | UI needs session token on mutating calls, moves/time/reward/next-stage result fields |
| Ranking | global/stage/me endpoints exist | UI needs category/segment support, my pinned row, display metadata |
| Progress | `GET /api/progress`, `POST /api/progress/batch` | Must stop exposing Domain entity directly; add shared DTOs |
| Auth/Profile | Services exist internally | No player-facing account/profile/session API |

---

## Required Screen Data

### Bootstrap

UI data:
- client version text
- loading step label
- loading progress 0..1
- server time / meta hash / protocol compatibility result
- login/session status

Server contract:
- [ ] `GET /api/bootstrap/config`
  - Response: `clientVersion`, `requiredClientVersion`, `protocolVersion`, `metaHash`, `serverTimeUtc`, `maintenance`
- [ ] Auth/session endpoints listed under Account

### Title

UI data:
- account linked status
- display name or guest state
- selected language
- app version

Server contract:
- [x] `GET /api/account/me`
- [ ] Auth guest/login/refresh/link: delegated to auth-server agent; dev environment uses mock/JWT stub

### Lobby - Stage Tab

UI data:
- profile icon/display name
- stamina current/max/next recharge
- soft currency amount
- current playable stage id
- current stage stars
- next locked stage previews
- play enabled/disabled
- daily challenge availability and reset timer
- Color Cup/event availability and remaining time

Server contract:
- [ ] `GET /api/lobby/state`
  - Response includes `profile`, `stamina`, `currency`, `progressSummary`, `currentStage`, `nextStages`, `dailyChallenge`, `seasonEvent`
- [ ] `GET /api/progress`
  - Response DTO: list of `stageId`, `stars`, `clearedAt`, `isUnlocked`

### Lobby - No Energy State

UI data:
- stamina `0/max`
- next recharge time
- refill CTA availability
- play disabled reason

Server contract:
- [ ] Covered by `GET /api/lobby/state` and `GET /api/stamina`
- [ ] `POST /api/stamina/refill` if paid/full refill differs from existing `extend`

### Lobby - Shop Tab

UI data:
- category tabs: Coins, Items, Bundles, NoAds
- product cards: icon id, name, quantity, price label, category, purchase type
- player soft currency amount

Server contract:
- [x] Shop Catalog: `GET /api/shop/catalog` + `shared/datas/outgame/outgame_shop_catalog.csv`
- [ ] IAP receipt verification: TODO
- [x] UI should use server catalog as source of truth and generated CSV only for local display metadata.

### Lobby - Ranking Tab

UI data:
- segment tabs: Friends, Global, Color Cup
- top rank card
- rank rows: rank, avatar/icon id, display name, score/value, isMe
- sticky my rank row
- pagination/cursor or top count

Server contract:
- [ ] Extend ranking DTOs with `avatarId` or `avatarUrl`, `isMe`, `category`, `metricLabel`, `nextCursor`
- [ ] `GET /api/ranking/global/score`
- [ ] `GET /api/ranking/global/stages`
- [ ] `GET /api/ranking/stage/{stageId}`
- [ ] Friends ranking: TODO unless social graph is available
- [ ] Color Cup ranking: TODO until event system exists

### Game

UI data:
- stage id/label
- move limit and moves remaining/used
- color progress total/connected
- item counts for Hint, Undo, Paint, Hammer, Brush/Shuffle
- session token
- timer/time limit if stage uses timer

Server contract:
- [ ] `POST /api/stage/{stageId}/start`
  - Response: `sessionToken`, `serverStartAt`, `stageId`, `moveLimit`, `timeLimitSeconds`, `itemCounts`, `staminaCurrent`
- [ ] `POST /api/stage/{stageId}/lock`
  - Request: `sessionToken`
- [ ] `POST /api/items/use`
  - Existing route; request must include `stageSessionToken`, item ids/quantities

### Stage Clear Popup

UI data:
- stage id
- stars
- coin reward
- star reward/progress reward
- moves used/max
- clear time
- score
- is best record
- next stage unlocked

Server contract:
- [ ] `POST /api/stage/{stageId}/end`
  - Request: `sessionToken`, `result`, `clientElapsedMs`, `movesUsed`
  - Response: `score`, `stars`, `adjustedElapsedMs`, `isBestRecord`, `softReward`, `softBalanceAfter`, `movesUsed`, `moveLimit`, `nextStageId`, `nextStageUnlocked`

### Buy Item Popup

UI data:
- featured item: item id, icon id, name, description, bundle quantity
- item grid entries: item id, icon id, owned quantity, purchase quantity
- price in soft currency
- player soft balance

Server contract:
- [x] Shop Catalog: `GET /api/shop/catalog`
- [x] Existing `POST /api/shop/purchase`
  - Response includes `productId`, `softBalanceAfter`, `inventoryUpdates`
- [x] Item display metadata can come from `shared/datas/ingame/ingame_item.csv` and generated outgame shop catalog.

### Energy Popup

UI data:
- stamina current/max
- next recharge
- ad reward amount
- refill amount
- refill soft cost
- soft balance

Server contract:
- [x] `GET /api/stamina`
  - Response includes `current`, `max`, `nextRechargeAt`
- [x] `POST /api/stamina/ad-reward`
  - Response: `current`, `max`, `added`, `nextRechargeAt` — `Max` field added to `StaminaAdRewardResponse`
- [x] `POST /api/stamina/refill`
  - Response: `current`, `max`, `added`, `softCost`, `softBalanceAfter`, `nextRechargeAt`

### Settings Popup

UI data:
- BGM enabled
- SFX enabled
- haptics enabled
- notifications enabled
- language
- account link status

Server contract:
- [ ] Settings sync is optional; local PlayerPrefs remains valid.
- [ ] If synced: `GET /api/player/settings`, `PUT /api/player/settings`
  - DTO fields: `bgmEnabled`, `sfxEnabled`, `hapticsEnabled`, `notificationsEnabled`, `language`
- [ ] Push token routes only if notifications are implemented.

### Daily Challenge Popup

UI data:
- today's N stage IDs (date-seeded random selection from all stages)
- play count progress (playCountToday / playCountTarget)
- reset time/countdown
- 7-day streak tiles: done/today/locked
- reward preview: soft currency, item rewards
- play enabled state

Design decision:
- N stages selected each day via date-seeded RNG (N = static config, e.g. `outgame_daily_challenge` CSV)
- No dedicated start route — use existing `POST /api/stage/{stageId}/start` for any of `todayStageIds`
- `StageService.End` detects if played stage is in today's set and increments `playCount`

Server contract:
- [x] `GET /api/daily-challenge`
  - Response: `todayStageIds` (List\<int\>), `playCountToday`, `playCountTarget`, `completedToday`, `canComplete`, `streakDays`, `resetAt`, `tiles`, `todayRewards`
  - `TodayStageIds` added to `DailyChallengeResponse` DTO
- ~~`POST /api/daily-challenge/{challengeId}/start`~~ — dropped; use existing stage start
- [x] `POST /api/daily-challenge/complete`
  - Response: `rewardsGranted`, `streakDays`, `softBalanceAfter`, `inventoryUpdates`

Outstanding server impl:
- [x] `DailyChallengeService.GetAsync` — date-seeded stage selection implemented via `DailyChallengeStageSelector`; `TodayStageIds` populated
- [x] `StageService.End` — sets `IsDailyChallengeStage` flag; `StageEndTransactionRepository` gates play_count increment on this flag

### Account Popup

UI data:
- linked/unlinked state
- provider list: Google, Apple, Facebook, Email
- display name
- account id or masked email

Server contract:
- [x] `GET /api/account/me`
  - Response: `userId`, `displayName`, `isGuest`, `linkedProviders`, `avatarId`, `createdAt`
- [ ] `POST /api/auth/guest` — delegated to auth-server agent; dev uses mock/JWT stub
- [ ] `POST /api/auth/login` — delegated to auth-server agent
- [ ] `POST /api/auth/refresh` — delegated to auth-server agent
- [ ] `POST /api/account/link` — delegated to auth-server agent
- [ ] Social provider details remain delegated to auth-server agent.

### Pause / Quit Popup

UI data:
- current stage id
- warning text variables: stamina loss amount
- resume/quit enabled state

Server contract:
- [ ] No required API if quitting simply abandons Redis session.
- [ ] Add `POST /api/stage/{stageId}/abandon` only if server must explicitly close active stage sessions.

### Reward / Video Ad Popup

UI data:
- reward title/type
- base reward amount
- multiplier option
- final granted rewards
- soft balance and inventory updates

Server contract:
- [ ] `POST /api/rewards/claim`
  - Request: `rewardSource`, `rewardToken`, `multiplier`
  - Response: `rewardsGranted`, `softBalanceAfter`, `inventoryUpdates`
- [ ] Ad validation details are delegated to Ad System/server agent.

---

## Shared DTO Work Items

- [x] Add `Bootstrap/BootstrapResponses.cs`
- [x] Add `Account/AccountRequests.cs`, `Account/AccountResponses.cs`
- [x] Add `Lobby/LobbyResponses.cs`
- [x] Add `Progress/ProgressRequests.cs`, `Progress/ProgressResponses.cs`
- [x] Extend `Stage/StageRequests.cs`, `Stage/StageResponses.cs`
- [x] Extend `Stamina/StaminaRequests.cs`, `Stamina/StaminaResponses.cs`
  - [x] `StaminaAdRewardResponse.Max` added
- [x] Extend `Currency/CurrencyResponses.cs`
- [x] Extend `Item/ItemResponses.cs`
- [x] Extend `Ranking/RankingResponses.cs` — `avatarId`, `isMe`, `category`, `metricLabel`, `nextCursor` added
- [x] Add `Daily/DailyChallengeRequests.cs`, `Daily/DailyChallengeResponses.cs`
  - [x] `DailyChallengeResponse.TodayStageIds: List<int>` added
- [x] Add `Reward/RewardRequests.cs`, `Reward/RewardResponses.cs`
- [x] Add `Settings/PlayerSettingsRequests.cs`, `Settings/PlayerSettingsResponses.cs`
- [x] Update `shared/contracts/AGENTS.md` after DTO files change
- [x] Copy/sync updated contracts to Unity `Assets/Scripts/Generated/Contracts/`

---

## Server Agent Handoff

Server implementation agent owns:
- route naming finalization
- controller/service/repository logic
- validation, idempotency, transactions, Redis TTLs
- DB schema updates
- auth-server/social/IAP/ad verification integration
- tests

This TODO intentionally specifies only UI-required data and DTO/API surfaces.

---

## UI Agent Handoff

UI implementation agent owns:
- updating `ProjectLinkUIBuilder` generated hierarchy to match PDF screens
- using server DTO data and generated CSV metadata for labels/counters/states; explicit debug adapters only for local tests
- keeping scene/popup controllers behind service interfaces
- avoiding direct controller/server class dependencies
- documenting changed UI builder symbols in `client/project-link/Assets/Scripts/Editor/AGENTS.md`

---

## Client Binding Prerequisites

Before replacing generated UI hierarchy:

- [x] Run `npm run gen:data` so outgame planning tables are available under Unity `Resources/Data/outgame`.
- [x] Sync `shared/contracts/*.cs` into Unity `Assets/Scripts/Generated/Contracts/`.
- [x] Make shared contract DTOs Unity-compatible without implicit SDK usings.
- [x] Add a real planning-table loader for outgame UI data.
- [x] Add client service interfaces and API route constants that UI controllers can depend on.
- [x] Implement HTTP adapters behind `IUiDataService`.
- [x] Add ViewModel/mapper layer that merges server DTOs with generated CSV metadata.
- [x] Add/confirm account controller route before Account popup binding.
- [x] Auth guest route uses dev mock token; login/refresh/link routes remain delegated to auth-server agent.
- [x] Add/confirm reward claim controller route before Reward/Video Ad popup binding.
- [x] Update `ProjectLinkUIBuilder` to generate bindable refs only, not hardcoded visible values.

<!-- changed: added UI wireframe server-contract tracker from Color Paths PDF -->
<!-- changed: added client data-binding prerequisites before visual UI rebuild -->
<!-- changed: ProjectLinkUIBuilder now emits PDF-aligned bindable labels/buttons and lobby/popup controllers bind via IUiDataService/static catalog. -->
