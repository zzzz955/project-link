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

## Client Boundary First

Before server sync, all UI/scene code must access progress through a service interface.

- [ ] Create `IProgressService` wrapper around current `DataManager`
- [ ] Move Lobby unlock/star reads behind `IProgressService`
- [ ] Move Tutorial `tutorialSeen` reads/writes behind `IProgressService` or settings boundary
- [ ] Add mock implementation with artificial delay/failure for UI testing
- [ ] Keep `DataManager` as the local persistence implementation

---

## Local Save

> `DataManager` (`Core/DataManager.cs`) already handles local save via PlayerPrefs + JsonUtility. No separate `SaveService` needed.

- [x] Stage progress: `ClearStage(stageId, stars)`, `IsStageCleared`, `GetStarRating`, `IsStageUnlocked`
- [x] Settings: `SoundVolume`, `SfxVolume`, `HapticEnabled`
- [x] Flags: `SetFlag(string key, bool value)`, `GetFlag(string key, bool defaultValue)` — implemented in `DataManager`
- [x] Load on Awake; save immediately on mutation
- [ ] Register `DataManager` prefab in Bootstrap DDL (if not already done)

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

<!-- changed: local progress now becomes the client-first adapter before server sync is wired -->
