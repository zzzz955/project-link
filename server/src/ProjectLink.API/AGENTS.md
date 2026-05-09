# ProjectLink.API

## Nav
| path | role |
|------|------|
| `Controllers/` | All HTTP endpoints | -> `Controllers/AGENTS.md` |
| `Middleware/` | Request pipeline: correlation ID, version check, meta-hash, session validation, global exception handler |
| `Program.cs` | ASP.NET Core entry point: DI registrations, middleware order |
| `MockAuthenticationHandler.cs` | Development/mock auth scheme for local guest tokens |

## Rules
- `userId` extracted from authenticated claims in every authenticated controller; never from request body.
- Unauthenticated endpoints: `GET /health`, `GET /api/bootstrap/config`, `GET /api/events/season`, `POST /api/auth/guest`.
- Middleware order in `Program.cs`: CorrelationId -> GlobalException -> HTTPS -> Auth -> Authorization -> RateLimit -> VersionCheck -> MetaHash -> SessionValidation -> Controllers.
