# 10 — Server (ASP.NET Core)

Stack: ASP.NET Core 8 | Entity Framework Core | PostgreSQL (TBD — confirm DB)
Location: `server/src/`

---

## Project Setup

- [ ] Create ASP.NET Core Web API project in `server/src/`
- [ ] Update `server/AGENTS.md` Nav table and Stack section with chosen stack
- [ ] Add NuGet packages: `Microsoft.EntityFrameworkCore`, DB provider, `Microsoft.AspNetCore.Authentication.JwtBearer`
- [ ] Configure `appsettings.json` + `.env` for: `DB_CONNECTION`, `JWT_AUTHORITY`, `JWT_AUDIENCE`, `JWT_APP_CLIENT_ID`
- [ ] Add `.env` vars to `.env.example` (no secrets in repo)
- [ ] Run `npm run gen:orm` to sync `StageProgress` table from `server/db/schema.json`

---

## Auth Middleware

- [ ] Add JWT Bearer authentication middleware
- [ ] Validate `iss` / `aud` against shared auth server
- [ ] Validate `app` claim == this game's `client_id` — return 403 if mismatch
- [ ] Inject `userId` (`sub` claim) into `HttpContext` for use in controllers

---

## Stage Progress API

- [ ] `GET /api/progress`
  - Auth required
  - Returns all `StageProgress` rows for authenticated `userId`
- [ ] `POST /api/progress/batch`
  - Auth required
  - Body: `[{ stageId, stars, clearedAt }]`
  - Upsert: insert new, update existing only if incoming `clearedAt` > stored `clearedAt`

---

## Infrastructure

- [ ] `docker-compose.yml`: app container + DB container for local dev
- [ ] `GET /health` endpoint — returns 200 OK (used for container health checks)
- [ ] `.env.example` with all required variables documented

---

## DB Schema

Source of truth: `server/db/schema.json`
Do not write or edit migration SQL manually — run `npm run gen:orm` after schema changes.
See `06_progression_save.md` for `StageProgress` schema definition.
