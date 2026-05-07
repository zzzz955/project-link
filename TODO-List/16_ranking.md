# 16 — Ranking System

## Spec
| key | value |
|-----|-------|
| Types | Per-stage time, Max stages cleared, Total score |
| Reset | None (all-time) |
| Scope | Global |
| Tie-break | Earlier account creation date wins (lower rank number) |
| Storage | Redis Sorted Sets (real-time) + DB (persistence) |

---

## Ranking Types

### 1. Per-Stage Time Ranking
- Metric: best clear time (ms) per stage per user — lower is better
- Redis key: `ranking:stage:{stageId}:time` — score = encoded time (see Tie-breaking)

### 2. Max Stages Cleared Ranking
- Metric: count of distinct stages ever cleared — higher is better
- Redis key: `ranking:global:stages` — score = encoded count

### 3. Total Score Ranking
- Metric: sum of best scores across all cleared stages — higher is better
- Redis key: `ranking:global:score` — score = encoded total score

---

## Scoring Formula

```
elapsed_cs      = floor(adjusted_elapsed_ms / 10)          // ms → centiseconds
score_per_stage = max(0, stage.timeLimit * 100 - elapsed_cs)
```

- `timeLimit` is already in `ingame_stage.csv` (seconds, int32) — no new CSV columns needed
- `timeLimit * 100` = max possible centiseconds = max score for that stage
- Example: timeLimit=60, cleared in 20s → elapsed_cs=2000, score=4000
- Fail or time-out → score not recorded (no entry created)
- Total score = sum of `best_score` across all `stage_best_records` rows for a user

---

## Network Latency Compensation

Client reports `client_elapsed_ms` (stopwatch). Server measures `server_elapsed_ms = stage_end_at - stage_start_at`.

Network discrepancy: `server_elapsed_ms ≈ client_elapsed_ms + RTT` (round-trip overhead).

Adjustment:
```
adjusted_elapsed_ms = max(client_elapsed_ms, server_elapsed_ms - NETWORK_TOLERANCE_MS)
```

- Legitimate player (fast network, e.g. RTT=100ms, TOLERANCE=2000ms):
  `max(30000, 30100 - 2000) = max(30000, 28100) = 30000` → client time used ✓
- Legitimate player (slow network, e.g. RTT=1800ms):
  `max(30000, 31800 - 2000) = max(30000, 29800) = 30000` → client time used ✓
- Cheat (reports 5s elapsed, actual 30s):
  `max(5000, 30100 - 2000) = max(5000, 28100) = 28100` → server-anchored time used ✓

Config key: `[Ranking] network_tolerance_ms = 2000`

---

## Tie-breaking via Encoded Score

Redis Sorted Sets use float64. Encode tie-break into the score so earlier account creation date wins automatically.

**Ascending rankings (lower = better, e.g. time ranking):**
```
encoded = elapsed_ms * 1e10 + account_created_at_unix
```
Same elapsed → smaller unix timestamp (earlier date) → smaller encoded → better rank ✓

**Descending rankings (higher = better, e.g. score, stages):**
```
MAX_UNIX_TS = 9999999999   // year 2286
encoded = primary_value * 1e10 + (MAX_UNIX_TS - account_created_at_unix)
```
Same primary → smaller unix timestamp (earlier date) → larger complement → larger encoded → better rank ✓

Precision constraint: `primary_value * 1e10` must fit in float64 mantissa (53-bit).
- Max safe `elapsed_ms` ≈ 900 seconds → 9e11 * 1e10 = 9e21 → exceeds float64 precision
- Solution: store `elapsed_cs` (centiseconds) as primary for time ranking
  - Max elapsed_cs for any stage ≈ 18000 (3min) → 1.8e4 * 1e10 = 1.8e14 → within 53-bit ✓
- Max safe `total_score`: stages=31, max_score_per_stage=18000 → total ≈ 558000 → 5.58e5 * 1e10 = 5.58e15 → within 53-bit ✓

---

## DB

### Table: `stage_best_records`
Best clear per user per stage — authoritative source for Redis rebuild.

| column | type | constraints |
|--------|------|-------------|
| user_id | string(36) | PK NN |
| stage_id | int32 | PK NN |
| best_clear_time_ms | int64 | NN |
| best_score | int32 | NN |
| cleared_at | datetime | NN |

Update rule: replace only if `new_clear_time_ms < existing best_clear_time_ms`.

### Table: `user_ranking_cache`
Aggregate snapshot — rebuilt from `stage_best_records` on Redis cold start.

| column | type | constraints |
|--------|------|-------------|
| user_id | string(36) | PK NN |
| total_score | int64 | NN |
| stages_cleared | int32 | NN |
| updated_at | datetime | NN |

### Table: `user_profiles`
Required for `account_created_at` used in tie-break encoding.
Populated by `SessionValidationMiddleware` on every authenticated request (Redis-cached after first insert).
`account_created_at` is set to server `NOW()` on first insert — no JWT claim required.

| column | type | constraints |
|--------|------|-------------|
| user_id | string(36) | PK NN |
| display_name | string(64) | NN |
| account_created_at | datetime | NN — server NOW() on first login |
| last_login_at | datetime | NN |

---

## API Endpoints

| method | path | auth | description |
|--------|------|------|-------------|
| GET | `/api/ranking/stage/{stageId}` | JWT | Top N for specific stage (by time, ascending) |
| GET | `/api/ranking/global/stages` | JWT | Top N by max stages cleared |
| GET | `/api/ranking/global/score` | JWT | Top N by total score |
| GET | `/api/ranking/me` | JWT | Caller's rank in all 3 categories |

Query param: `?top=100` (default 100, max 500).

### Response shape
```json
{
  "entries": [
    { "rank": 1, "user_id": "...", "display_name": "...", "value": 12500 }
  ],
  "my_rank": { "rank": 42, "value": 18300 }
}
```

---

## Stage Clear Flow Integration

```
POST /api/stage/{id}/end  { result: "success", client_elapsed_ms: 20000 }
  → load stage data → get timeLimit
  → server_elapsed_ms = now - session.start_at
  → adjusted_elapsed_ms = max(client_elapsed_ms, server_elapsed_ms - NETWORK_TOLERANCE_MS)
  → elapsed_cs = floor(adjusted_elapsed_ms / 10)
  → score = max(0, timeLimit * 100 - elapsed_cs)
  → if new time < stage_best_records.best_clear_time_ms → upsert record + update cache + ZADD Redis (all 3 keys)
  → upsert stage_progress (stars) — independent of ranking
```

---

## Redis Rebuild (Cold Start)

On startup, if ranking keys are absent:
1. Load all `stage_best_records` + `user_ranking_cache` + `user_profiles`
2. For each user × stage: ZADD `ranking:stage:{id}:time` with encoded elapsed_cs
3. For each user: ZADD `ranking:global:stages` and `ranking:global:score` with encoded values

---

## Tasks

- [x] Add tables `stage_best_records`, `user_ranking_cache`, `user_profiles` to `server/db/schema.json` → run `npm run gen:orm` *(schema already updated)*
- [x] `RankingService.ComputeScore(timeLimit, adjustedElapsedMs)` — formula + centisecond conversion
- [x] `RankingService.OnStageClear(userId, stageId, clientElapsedMs, serverElapsedMs)` — adjust → score → upsert DB → ZADD Redis
- [x] `RankingService.GetStageRanking(stageId, top)` — ZRANGE + decode encoded score
- [x] `RankingService.RebuildFromDB()` — cold start rebuild via `RankingRebuildHostedService`
- [x] `GET /api/ranking/*` controllers
- [x] `GET /api/ranking/me` controller
- [x] `RankingRequests.cs` / `RankingResponses.cs` in `shared/contracts/Ranking/`

<!-- changed: scoring formula revised — uses existing timeLimit column, no new CSV columns; network compensation added -->
