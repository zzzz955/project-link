# 05 - Auth System

Architecture: Shared auth server reused across all games.
Each game is a separate registered app (`client_id`); tokens from one game are invalid in another.

---

## Part 0 - Client Mock Auth First

Used while client scene/UI flow is still being finalized.

- [x] Define `IAuthService` before implementing HTTP auth
- [x] Implement `MockAuthService`
  - [x] guest login success (`MockAuthScenario.Success`)
  - [x] login failure (`MockAuthScenario.Failure`)
  - [x] refresh success
  - [x] session expired (`MockAuthScenario.SessionExpired`)
- [ ] Title/Lobby flow depends only on `IAuthService`
- [x] Add Auth service mode switch: `MockAuthService` injected at Bootstrap; replace with `HttpAuthService` for Http mode
- [ ] Session expired UI path works without a real auth server

---

## Part A - Shared Auth Server (separate repo)

> Standalone service, not part of this game's server.
> Stack: ASP.NET Core 8 | ASP.NET Core Identity | JWT Bearer | PostgreSQL

- [ ] Create `auth-server` as a separate repository/project
- [ ] DB tables: `Users`, `AppClients` (one row per registered game)
- [ ] Endpoints:
  - `POST /auth/register` - email + password, scoped to `client_id`
  - `POST /auth/login` - returns `access_token` (JWT) + `refresh_token`
  - `POST /auth/refresh` - rotate access + refresh tokens
  - `POST /auth/guest` - create anonymous account, return JWT
  - `POST /auth/link` - upgrade guest account to registered (merge progress)
  - `POST /auth/social/google-play` - accept Google Play Games token, return JWT
- [ ] JWT claims: `sub` (userId GUID), `app` (client_id), `role`
- [ ] Token validation: each game server checks `app` claim matches its own `client_id`; reject otherwise
- [ ] Register this game's `client_id` in `AppClients` table

---

## Part B - This Game: Auth Client (Unity)

- [ ] Implement `AuthService` in `Core/`
  - `GuestLoginAsync()` - call `POST /auth/guest`, store JWT
  - `LoginAsync(email, password)` - call `POST /auth/login`, store JWT
  - `RefreshAsync()` - call `POST /auth/refresh` on 401 response
  - Store JWT + refresh token in `PlayerPrefs` (encrypted with device key)
- [ ] Google Play Games sign-in
  - [ ] Integrate Play Games Plugin for Unity (v11+)
  - [ ] On Play Games sign-in success: call `POST /auth/social/google-play` with server-auth-code
  - [ ] Exchange for JWT and store
- [ ] Guest -> registered account upgrade
  - [ ] Prompt after 5 sessions (configurable)
  - [ ] UI: email/password registration form in a popup
  - [ ] On success: call `POST /auth/link`
- [ ] Attach `Authorization: Bearer <token>` header in all `NetworkManager` requests

<!-- changed: auth work reordered so client can finish against mock auth before shared auth server integration -->
