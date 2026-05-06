# 10 — Server (ASP.NET Core)

Stack: ASP.NET Core 8 Web API | EF Core 8 (ORM-only, no migrations) | PostgreSQL | Redis
Location: `server/src/`
Schema source of truth: `server/db/schema.json` — run `npm run gen:orm` to sync DB.
EF Core is used as ORM only — never run `dotnet ef migrations`. Schema is exclusively managed by gen-orm.

---

## Attachment Gate

Do not wire gameplay scenes directly to this server until `00_client_first_server_boundary.md` is satisfied.

- [ ] Match server DTOs to client `IAuthService` / `IProgressService` contracts
- [ ] Keep API routes stable before replacing mock/local client adapters
- [ ] Validate auth/progress with generated or mirrored packet DTOs
- [ ] Add edge-proxy/auth-server integration after client flow is stable

---

## Project Structure

```
server/src/
├── ProjectLink.API/
│   ├── Controllers/
│   │   ├── HealthController.cs
│   │   └── ProgressController.cs
│   ├── Middleware/
│   │   ├── CorrelationIdMiddleware.cs
│   │   ├── VersionCheckMiddleware.cs
│   │   ├── MetaHashMiddleware.cs
│   │   └── SessionValidationMiddleware.cs
│   ├── appsettings.json
│   ├── Dockerfile
│   └── Program.cs
├── ProjectLink.Application/
│   ├── Progress/
│   │   ├── GetProgressQuery.cs
│   │   └── UpsertProgressCommand.cs
│   ├── Session/
│   │   └── SessionService.cs
│   └── Currency/               # placeholder — no currency in this game, template only
├── ProjectLink.Domain/
│   ├── Entities/
│   │   ├── StageProgress.cs
│   │   ├── Session.cs
│   │   └── CurrencyLog.cs
│   └── Interfaces/
│       ├── IProgressRepository.cs
│       └── ISessionRepository.cs
├── ProjectLink.Infrastructure/
│   ├── Persistence/
│   │   ├── AppDbContext.cs
│   │   ├── ProgressRepository.cs
│   │   └── SessionRepository.cs
│   ├── Cache/
│   │   └── RedisSessionCache.cs
│   └── Security/
│       └── JwtPublicKeyCache.cs
└── docker-compose.yml
```

---

## Setup Tasks

- [ ] Create solution (`ProjectLink.sln`) and 4 `.csproj` projects with correct project references
- [ ] NuGet packages:
  - `Npgsql.EntityFrameworkCore.PostgreSQL`
  - `Microsoft.EntityFrameworkCore`
  - `Microsoft.AspNetCore.Authentication.JwtBearer`
  - `StackExchange.Redis`
  - `Scalar.AspNetCore`
- [ ] `appsettings.json` keys: `ConnectionStrings:Postgres`, `Redis:Connection`, `Jwt:Authority`, `Jwt:Audience`, `App:ClientId`, `App:AllowedClientVersion`, `App:AllowedProtocolVersion`
- [ ] `.env.example` with all vars (no secrets in repo)
- [ ] `docker-compose.yml`: `api` (port 8080) + `db` (PostgreSQL 16) + `redis` (Redis 7) with named volumes
- [ ] `Dockerfile` for API project (multi-stage build)

---

## Security Middleware Stack

Middleware registration order in `Program.cs`:
`CorrelationId → VersionCheck → Authentication → SessionValidation → MetaHash (on designated endpoints)`

### CorrelationIdMiddleware
- [ ] Read `X-Correlation-ID` header; generate UUID if absent
- [ ] Store in `HttpContext.Items["CorrelationId"]`; add to response header
- [ ] All log entries must include this value

### VersionCheckMiddleware
- [ ] Read `X-Client-Version` and `X-Protocol-Version` headers
- [ ] Compare against `App:AllowedClientVersion` and `App:AllowedProtocolVersion` config
- [ ] Mismatch → `426 Upgrade Required` `{ "reason": "version_mismatch", "required": "x.y.z" }`
- [ ] Skip for `/health`

### SessionValidationMiddleware (authenticated routes only)
- [ ] Extract `session_id` claim from JWT
- [ ] Look up `session:{userId}` in Redis (TTL = token lifetime)
- [ ] Redis miss → fall back to `sessions` DB table, repopulate Redis
- [ ] `session_id` mismatch (another device logged in) → `401` `{ "reason": "session_invalidated" }`

### MetaHashMiddleware
- [ ] Applied only to `POST /auth/session` (initial session creation)
- [ ] Read `X-Meta-Hash` header
- [ ] Compare against `client_meta.meta_hash` for the given client version
- [ ] Mismatch → `403` `{ "reason": "integrity_check_failed" }`

---

## Auth Integration

- [ ] JWT Bearer middleware configured in `Program.cs`
- [ ] `JwtPublicKeyCache`: fetch JWKS from `{Jwt:Authority}/.well-known/jwks.json` on startup; refresh every 24h
- [ ] Validate `app` claim == `App:ClientId` — `403` if mismatch

---

## Session Management

Login flow (called by game server after auth-server validates credentials):
- [ ] `SessionService.CreateSession(userId)`:
  - Generate new `session_id` (UUID)
  - Invalidate any existing active session for this userId (DB update + Redis delete)
  - Insert new row in `sessions` table
  - Store `session:{userId}` = `session_id` in Redis (TTL = access token lifetime)
  - Returns new `session_id`
- [ ] `SessionService.InvalidateSession(userId)`: mark DB session inactive + delete Redis key

---

## JWT Token Family (Refresh Token Rotation)

Handled primarily in auth-server. Game server responsibilities:
- [ ] `POST /api/auth/refresh` → forward refresh token to auth-server, update Redis session on success
- [ ] Auth-server detects family reuse → returns `401` → game server deletes Redis session, returns `401` to client

---

## Stage Progress API

- [ ] `GET /api/progress` (auth required)
  - Returns all `StageProgress` rows for authenticated `userId`
- [ ] `POST /api/progress/batch` (auth required)
  - Body: `[{ stageId, stars, clearedAt }]`
  - Upsert: keep record with later `clearedAt` (server is source of truth)
  - Wrap in DB transaction; emit `transaction_id` to logs

---

## Currency Guard (Template)

No currency in this game. Implement as a placeholder for template reuse.
- [ ] `CurrencyService.Deduct(userId, amount, reason, transactionId)`:
  - `UPDATE currency SET balance = balance - @amount WHERE user_id = @userId AND balance >= @amount`
  - Rows affected == 0 → throw `InsufficientFundsException` (never go negative)
  - On success → append to `currency_logs`

---

## Log System

### Correlation ID
- [ ] All structured log entries include `correlationId` (from middleware)
- [ ] Use `Microsoft.Extensions.Logging` with structured output (JSON format in production)

### Transaction Mapping Key
- [ ] Each business transaction generates a `transaction_id` (UUID) at the use-case layer
- [ ] All DB writes within one transaction share the same `transaction_id` in log output

### Currency Audit Log (append-only, immutable)
- [ ] `currency_logs` table — never UPDATE or DELETE rows
- [ ] Schema: `id, user_id, transaction_id, currency_type, delta, balance_before, balance_after, reason, correlation_id, created_at`

---

## DB Schema

File: `server/db/schema.json` — create this file with tables below, then run `npm run gen:orm`.

Tables:
- `sessions`: `id(PK AUTO), user_id(string NN), session_id(string(36) NN UQ), created_at(datetime NN), expires_at(datetime NN), active(bool NN DEFAULT true)`
- `stage_progress`: `user_id(string NN FK:users), stage_id(int32 NN), stars(int32 NN), cleared_at(datetime NN)` — PK: `(user_id, stage_id)`
- `client_meta`: `client_version(string(20) PK NN), meta_hash(string NN), protocol_version(string(20) NN), created_at(datetime NN)`
- `currency_logs`: `id(PK AUTO), user_id(string NN), transaction_id(string(36) NN), currency_type(string(32) NN), delta(int64 NN), balance_before(int64 NN), balance_after(int64 NN), reason(string NN), correlation_id(string(36) NN), created_at(datetime NN)`

Note: `users` table lives in auth-server DB — `user_id` here is a string FK by convention (no enforced FK across DBs).

---

## Infrastructure

- [ ] `GET /health` — returns 200 + `{ "db": "ok", "redis": "ok" }` (ping both)
- [ ] `docker-compose.yml` with health checks on db and redis services
- [ ] Named volumes for data persistence

<!-- changed: server work is now gated behind stabilized client service contracts -->
