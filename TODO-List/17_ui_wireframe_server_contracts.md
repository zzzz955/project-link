# 17 - UI Wireframe Server Contracts

Goal: update all `ProjectLinkUIBuilder` generated UI to match `Color Paths - Unity6 Wireframes`, but do not bind the new UI to HTTP until the server DTO/API contract is stable.

Reference:
- `C:/Users/SangHyeok/Downloads/Color Paths * Unity Wireframes.pdf`
- Client builder: `client/project-link/Assets/Scripts/Editor/ProjectLinkUIBuilder.cs`
- Shared DTO source: `shared/contracts/`
- Server API: `server/src/ProjectLink.API/Controllers/`

---

## Direction

- [ ] Treat the PDF as the UI source of truth for generated scene/popup hierarchy.
- [ ] Keep UI work mock/local-service driven until DTO/API shapes below are available.
- [ ] Server agents should define DTOs in `shared/contracts/` first, then implement controllers.
- [ ] UI agents should bind only to service interfaces and mock data until routes and DTOs are stable.
- [ ] Detailed server business rules are delegated to the server implementation agent.

---

## Contract Gate Before New UI Binding

The new UI can be generated visually before this gate, but real server binding starts only after:

- [ ] `Auth`, `Player/Lobby`, `Progress`, `Stage`, `Stamina`, `Currency`, `Item`, `Ranking`, `Daily`, `Reward`, `Settings` DTOs needed below are present or explicitly deferred.
- [ ] Controller routes are named and stable.
- [ ] Each response contains enough data for PDF labels, counters, disabled states, badges, timers, and popups.
- [ ] Error responses remain unified through `Common/ErrorResponse`.
- [ ] Unity-side mock adapters can construct every response shape without a running server.

---

## Existing Coverage

| Domain | Existing | Gap |
|---|---|---|
| Stamina | `GET /api/stamina`, `POST /api/stamina/ad-reward`, `POST /api/stamina/extend` | Refill popup needs explicit reward/cost/max/current data aligned to UI |
| Currency | `GET /api/currency`, `POST /api/currency/ad-reward` | Lobby/reward popup needs grant result DTO consistency |
| Inventory/Items | `GET /api/inventory`, `POST /api/items/purchase`, `POST /api/items/use` | Shop catalog is TODO; BuyItem popup still needs item display data from static CSV/mock |
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
- [ ] `GET /api/account/me`
- [ ] account link/login endpoints listed under Account

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
- [ ] Shop Catalog: TODO
- [ ] IAP receipt verification: TODO
- [ ] For now, UI should use static CSV/mock product data and existing item purchase route only.

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
- [ ] Shop Catalog: TODO
- [ ] Existing `POST /api/items/purchase`
  - Response should include `itemId`, `quantityAfter`, `softBalanceAfter`
- [ ] Item display metadata can come from `shared/datas/ingame/ingame_item.csv` until Shop Catalog is implemented.

### Energy Popup

UI data:
- stamina current/max
- next recharge
- ad reward amount
- refill amount
- refill soft cost
- soft balance

Server contract:
- [ ] `GET /api/stamina`
  - Ensure response includes `current`, `max`, `nextRechargeAt`
- [ ] `POST /api/stamina/ad-reward`
  - Response: `current`, `max`, `added`, `nextRechargeAt`
- [ ] `POST /api/stamina/refill`
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
- today's challenge id/name/stage id
- reset time/countdown
- 7-day streak tiles: done/today/locked
- reward preview: soft currency, item rewards
- play enabled state

Server contract:
- [ ] `GET /api/daily-challenge`
  - Response: `challengeId`, `stageId`, `name`, `resetAt`, `streakDays`, `tiles`, `rewards`, `canPlay`, `completedToday`
- [ ] `POST /api/daily-challenge/{challengeId}/start`
  - Response: `stageId`, `sessionToken`, `serverStartAt`
- [ ] `POST /api/daily-challenge/{challengeId}/complete`
  - Response: `rewardsGranted`, `streakDays`, `softBalanceAfter`, `inventoryUpdates`

### Account Popup

UI data:
- linked/unlinked state
- provider list: Google, Apple, Facebook, Email
- display name
- account id or masked email

Server contract:
- [ ] `GET /api/account/me`
  - Response: `userId`, `displayName`, `isGuest`, `linkedProviders`, `avatarId`, `createdAt`
- [ ] `POST /api/auth/guest`
- [ ] `POST /api/auth/login`
- [ ] `POST /api/auth/refresh`
- [ ] `POST /api/account/link`
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

- [ ] Add `Bootstrap/BootstrapResponses.cs`
- [ ] Add `Account/AccountRequests.cs`, `Account/AccountResponses.cs`
- [ ] Add `Lobby/LobbyResponses.cs`
- [ ] Add `Progress/ProgressRequests.cs`, `Progress/ProgressResponses.cs`
- [ ] Extend `Stage/StageRequests.cs`, `Stage/StageResponses.cs`
- [ ] Extend `Stamina/StaminaRequests.cs`, `Stamina/StaminaResponses.cs`
- [ ] Extend `Currency/CurrencyResponses.cs` only if reward grant shape is reused
- [ ] Extend `Item/ItemResponses.cs` for display/quantity updates if needed
- [ ] Extend `Ranking/RankingResponses.cs`
- [ ] Add `Daily/DailyChallengeRequests.cs`, `Daily/DailyChallengeResponses.cs`
- [ ] Add `Reward/RewardRequests.cs`, `Reward/RewardResponses.cs`
- [ ] Add `Settings/PlayerSettingsRequests.cs`, `Settings/PlayerSettingsResponses.cs` only if settings sync is implemented
- [ ] Update `shared/contracts/AGENTS.md` after DTO files change
- [ ] Copy/sync updated contracts to Unity generated contracts after DTOs stabilize

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
- using mock DTO data for all labels/counters/states before HTTP binding
- keeping scene/popup controllers behind service interfaces
- avoiding direct controller/server class dependencies
- documenting changed UI builder symbols in `client/project-link/Assets/Scripts/Editor/AGENTS.md`

<!-- changed: added UI wireframe server-contract tracker from Color Paths PDF -->
