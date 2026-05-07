# 13 — Stamina System

## Spec
| key | value |
|-----|-------|
| Max | 5 |
| Recharge | 1 per 30 min (lazy evaluation) |
| Cost | 1 per stage FAIL (success = no deduction) |
| Refill | Ad (+1 or full), Purchase (full) |
| Extension | Pay soft currency → retry without stamina loss (replaces fail deduction) |

Lazy eval: on read, compute `current = min(max, stored + floor((now - last_recharged_at) / 30min))`.  
No background timer — DB stores last known state only.

---

## DB

Table: `stamina_state`

| column | type | constraints |
|--------|------|------------|
| user_id | string(36) | PK NN |
| current | int8 | NN |
| last_recharged_at | datetime | NN |

- `current` = value at time of last write (NOT real-time)
- `last_recharged_at` = timestamp of last recharge tick committed to DB

---

## API Endpoints

| method | path | auth | description |
|--------|------|------|-------------|
| GET | `/api/stamina` | JWT | Returns computed current stamina (lazy calc) |
| POST | `/api/stamina/ad-reward` | JWT | Client reports ad completion → +1 stamina (idempotent via ad_token) |
| POST | `/api/stamina/extend` | JWT | Pay currency → skip fail deduction for current session |

### GET /api/stamina response
```json
{ "current": 3, "max": 5, "next_recharge_at": "2025-01-01T12:30:00Z" }
```
`next_recharge_at`: null if full.

### POST /api/stamina/extend
- Validates: active stage session exists, user has sufficient currency
- Atomic: deduct currency + mark session as "extended" (no stamina deduct on fail)
- Body: `{ "currency_amount": 30 }` (amount read from server config, not client)

### POST /api/stamina/ad-reward
- Body: `{ "ad_token": "<platform-issued token>" }` — server validates token (or trusts platform callback)
- Idempotent by `ad_token`: second call with same token → 200 with no-op
- Reward amount (1 or full) read from server config

---

## Stage Flow Integration

```
POST /api/stage/{id}/start
  → check stamina >= 1 (if not extended)
  → create stage session in Redis (TTL = max stage time + buffer)

POST /api/stage/{id}/end  { result: "fail" | "success", clear_time_ms }
  → validate session
  → if FAIL and NOT extended → deduct 1 stamina (lazy-write: flush current to DB)
  → if FAIL and extended    → no stamina change, currency already deducted
  → if SUCCESS              → no stamina change
  → delete session from Redis
```

---

## Config Keys (template.ini / .env)

```
[Stamina]
max=5
recharge_interval_minutes=30
ad_reward_amount=1
extend_cost_soft=30
```

---

## Tasks

- [x] Add `stamina_state` table to `server/db/schema.json` → run `npm run gen:orm`
- [x] `StaminaService`: lazy recharge via SELECT FOR UPDATE in repository
- [x] `GET /api/stamina` controller
- [x] `POST /api/stamina/extend` controller (atomic with currency deduction)
- [x] `POST /api/stamina/ad-reward` controller (Redis SETNX idempotency)
- [x] Integrate stamina deduction into stage start flow
- [x] `StaminaRequests.cs` / `StaminaResponses.cs` in `shared/contracts/Stamina/`

<!-- changed: stamina system defined — fail-only deduction, lazy eval, ad/currency refill -->
