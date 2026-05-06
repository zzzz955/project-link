# 06 — Progression Save

Two layers: local (immediate, offline-safe) + server sync (authoritative on login).

---

## Data Model

```
StageProgressData {
  stageId    : int
  cleared    : bool
  stars      : int       // 0–3
  clearedAt  : DateTime  // UTC
}
```

---

## Local Save

- [ ] Implement `SaveService` in `Core/`
  - `SaveStageProgress(stageId, stars)` — write to local JSON
  - `GetStageProgress(stageId) → StageProgressData?`
  - `GetAllProgress() → List<StageProgressData>`
  - `SetFlag(string key, bool value)` — for tutorial-seen, etc.
- [ ] Storage: JSON file at `Application.persistentDataPath/save.json`
- [ ] Load on Bootstrap startup; save immediately on stage clear
- [ ] `SaveService` registered in Bootstrap DDL

---

## Server Sync

Requires: `AuthService` JWT, game server running (`10_server.md`).

- [ ] Implement `ProgressSyncService` in `Core/`
  - `SyncOnLogin()` — fetch `GET /api/progress`, merge with local by latest `clearedAt`
  - `PushProgress(stageId, stars)` — fire-and-forget `POST /api/progress/batch`
- [ ] Offline queue: if push fails, append to `pendingSync.json`; retry on next session open
- [ ] Merge rule: for each `stageId`, keep the record with the later `clearedAt`

---

## DB Schema Entry

Add to `server/db/schema.json`:

```json
"StageProgress": {
  "userId":    "string, FK → Users.id",
  "stageId":   "int",
  "stars":     "int",
  "clearedAt": "datetime",
  "PK":        ["userId", "stageId"]
}
```

Run `npm run gen:orm` after updating schema.
