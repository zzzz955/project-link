# ProjectLink.API

## Nav
| path | role |
|------|------|
| `Controllers/` | All HTTP endpoints | → `Controllers/AGENTS.md` |
| `Middleware/` | Request pipeline: correlation ID, version check, meta-hash, session validation, global exception handler |
| `Program.cs` | ASP.NET Core entry point — DI registrations, middleware order |

## Rules
- `userId` extracted from JWT claim `sub` in every authenticated controller — never from request body
- Unauthenticated endpoints: `GET /health`, `GET /api/bootstrap/config`, `GET /api/events/season`
- Middleware order in `Program.cs`: CorrelationId → GlobalException → VersionCheck → MetaHash → Auth → SessionValidation → Controllers
