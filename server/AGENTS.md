# server — ASP.NET Core 8 | C# | Entity Framework Core

## Stack
ASP.NET Core 8 Web API | C# | Entity Framework Core 8 (ORM only, no migrations) | Pomelo MySQL | StackExchange.Redis | JWT Bearer

## Nav
| path | role |
|------|------|
| `db/` | DB schema definition + migration history | → `db/AGENTS.md` |
| `src/ProjectLink.sln` | Solution file |
| `src/ProjectLink.Domain/` | Entities, interfaces — no dependencies |
| `src/ProjectLink.Application/` | Use cases (commands/queries), ISessionCache |
| `src/ProjectLink.Infrastructure/` | EF Core DbContext, repositories, Redis cache, JWT key cache |
| `src/ProjectLink.API/` | Startup, controllers, middleware, Dockerfile |
| `tests/` | Engine-free server/API test projects | -> `tests/AGENTS.md` |
| `generated/` | Auto-generated — DO NOT edit | → see Rules |
| `../.env.dev.example` | Dev environment variable template |
| `../.env.prod.example` | Prod environment variable template |

## Rules
- NEVER edit `*/generated/*` — source is in `shared/`
- EF Core is used as ORM ONLY — never run `dotnet ef migrations` or `dotnet ef database update`
- DB schema is managed by `npm run gen:orm` (reads `server/db/schema.json`)
- NEVER commit `.env.dev` or `.env.prod` — use `.env.dev.example` / `.env.prod.example`
- NEW_DIR: create `AGENTS.md` for it + update Nav above

## DB Schema
File: `server/db/schema.json`
CMD:  `npm run gen:orm` — reads schema.json — syncs tables on connected DB
Migration SQL is saved to `server/db/migrations/` (review before applying in production)

## Project References
API → Application → Domain
Infrastructure → Domain
API → Infrastructure

## Cross-refs
| type | refs |
|------|------|
| Depends on | `project-link:docs/refs/platform-auth.md`, `project-link:docs/refs/platform-infra.md` |
| Platform source | `platform:docs/refs/auth.md` (`../platform/docs/refs/auth.md`) |
| External API | `platform-auth:GET /.well-known/jwks.json`, `platform-auth:POST /auth/refresh` |
| Local responsibility | Validate platform JWTs offline; do not own account identity or refresh-token family state. |
| Auth mode | `AUTH_USE_MOCK=true` → `MockAuthenticationHandler`; `false` → `JwtPublicKeyCache` + JWKS |

## Serena
FIND: `[Domain][Type].cs` → `find_symbol('[Domain][Type]')`
PATTERN: namespace `ProjectLink.[Layer]` or `ProjectLink.[Layer].[Domain]`
ENTRY: `src/ProjectLink.Domain/` — entities and interfaces
ENTRY: `src/ProjectLink.Application/` — handlers and services
ENTRY: `src/ProjectLink.Infrastructure/` — repositories and cache

## Conventions
- Namespaces: `ProjectLink.{Layer}` or `ProjectLink.{Layer}.{Domain}`
- No comments unless WHY is non-obvious
- `async/await` throughout — no `.Result` or `.Wait()`
- CancellationToken passed through all async methods
- Column names mapped to snake_case in `OnModelCreating`
