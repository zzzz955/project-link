# 19 - Integration Roadmap

Phase-ordered execution plan from auth-complete platform to production-ready game.
Platform auth (server, DB, containers) is complete as of 2026-05-12.

---

## Phase Status

| Phase | Description | Gate | Status |
|-------|-------------|------|--------|
| P0 | Dev environment running | — | DONE |
| P1 | Client core system | P0 | TODO |
| P2 | Game server ↔ auth server integration | P0 | TODO |
| P3 | Mock end-to-end verification | P1 | TODO |
| P4 | Full API wiring + UI rendering | P2 + P3 | TODO |
| P5 | Client OIDC + Google Auth | P4 (partial) | TODO |

P1 and P2 can run in parallel.

---

## P0 - Dev Environment (DONE)

Platform containers (auth server, DB, Redis) running via Docker Compose.
Game server containers (API, DB, Redis) running via Docker Compose.
See `docs/refs/platform-infra.md` for startup commands and port policy.

---

## P1 - Client Core System

Goal: all client scene/UI code depends only on interfaces; no direct HTTP calls anywhere.
Ref: `00_client_first_server_boundary.md`, `05_auth_system.md (Part 0)`

- [x] Define `IAuthService` (EnsureAuth, Refresh, GetToken, SetToken, ClearToken)
- [x] Define 401 handling in `NetworkManager` (clears token on 401; delegates to IAuthService)
- [x] Implement `MockAuthService` — Success / Failure / SessionExpired scenarios
- [x] Auth service defaults to `MockAuthService` on `NetworkManager.Awake`; injectable via `AuthService` property
- [x] `BootstrapEntry` injects `MockAuthService` into `NetworkManager` at startup
- [ ] Title → Lobby → InGame scene flow wired through `IAuthService` only
- [ ] Session-expired UI path works without real auth server

**Gate:** all items checked before P3 starts.

---

## P2 - Game Server ↔ Auth Server Integration

Goal: game server validates real JWTs from platform auth in dev.
This phase has no client dependency — verify via curl or Postman.

- [x] `JwtPublicKeyCache` fetches JWKS from `{Jwt:Authority}/.well-known/jwks.json` (already wired in Program.cs)
- [x] `AUTH_USE_MOCK=false` path configured in `Program.cs` JWT Bearer + app-claim validation
- [x] `POST /api/auth/guest` proxies to `{Jwt:Authority}/auth/guest` when `Auth:UseMock=false`
- [ ] Switch `.env.dev` to `AUTH_USE_MOCK=false` and verify JWKS fetch succeeds (requires platform stack running)
- [ ] Authenticated endpoint (e.g. `GET /api/lobby`) accepts platform-issued JWT
- [ ] 401 returned for expired / invalid JWT
- [ ] Integration test: guest login → lobby fetch full round-trip

**Gate:** all items checked before P4 starts.

---

## P3 - Mock End-to-End Verification

Goal: full client core loop works in mock mode before any real server calls.
Depends on: P1.

- [ ] Client `MockAuthService` → game server `AUTH_USE_MOCK=true` → Lobby response renders
- [ ] Stage Start → Stage End → progress stored via mock `IProgressService`
- [ ] Session expired → refresh → request retry flow works end-to-end in client
- [ ] UI loading and error states render correctly for auth, lobby, and stage domains

**Gate:** all items checked before P4 starts.

---

## P4 - Full API Wiring + UI Rendering

Goal: every domain wired between real game server and client UI.
Depends on: P2 + P3. Server domain impl and client UI can be parallelized per domain.

### Server — Application/Domain service layers

- [ ] Lobby — composite state assembly (profile, stamina, currency, progress snapshot)
- [ ] Stage — start session, validate, end session, persist best record
- [ ] Stamina — recharge timer, consume on stage start, ad refill
- [ ] Currency — balance read/write, ad reward
- [ ] Item — purchase, use, inventory query
- [ ] Progress — batch update, stage unlock gate
- [ ] Daily — challenge completion, streak tracking, reward preview
- [ ] Ranking — score submit, leaderboard fetch, my-rank query
- [ ] Shop — catalog serve, purchase flow (soft currency + IAP)
- [ ] Reward — claim validation, reward dispatch
- [ ] Settings — PATCH persist, read on login

### Client — HTTP adapters + UI binding

- [ ] Replace `MockAuthService` with `HttpAuthService`
- [ ] Lobby: fetch full game state on login, render OutGame UI
- [ ] Stage: bind start/end to server; render result screen with server data
- [ ] Stamina: real-time display, consume/refill via API
- [ ] Currency: display balance, ad reward flow
- [ ] Item: inventory UI, purchase and use flow
- [ ] Progress: stage unlock gates driven by server data
- [ ] Daily: challenge card, completion, claim flow
- [ ] Ranking: leaderboard scroll UI
- [ ] Shop: catalog render, purchase flow
- [ ] Reward: claim button, result popup
- [ ] Settings: persist to server on change; load on login

**Gate:** all server + client items checked.

---

## P5 - Client OIDC + Google Auth

Goal: Google Sign-In replaces guest auto-login for returning players.
Depends on: P4 (auth flow fully wired, platform OIDC endpoints live).

- [ ] Integrate Play Games Plugin for Unity (v11+)
- [ ] Implement `SocialLoginRequest` flow: Google Sign-In success → server-auth-code → platform JWT
- [ ] `AuthService.SocialLoginAsync(GooglePlayToken)` implementation
- [ ] Guest → registered account upgrade prompt (after N sessions, configurable)
- [ ] `POST /auth/link` wired for account merge
- [ ] Apple Sign-In (optional; required for iOS release)

**Gate:** all items checked.

---

## Cross-references

| Phase | Related TODO files |
|-------|-------------------|
| P1 | `00_client_first_server_boundary.md`, `05_auth_system.md` (Part 0) |
| P2 | `05_auth_system.md` (Part A), `18_infrastructure.md` |
| P3 | `00_client_first_server_boundary.md`, `01_ingame_ui.md` |
| P4 | `10_server.md`, `02_outgame_ui.md`, `06_progression_save.md`, `13_stamina.md`, `14_currency.md`, `15_items.md`, `16_ranking.md`, `17_ui_wireframe_server_contracts.md` |
| P5 | `05_auth_system.md` (Part B) |

<!-- changed: created as integration roadmap after platform auth system completion 2026-05-12 -->
