# ProjectLink.API

## Nav
| path | role |
|------|------|
| `Controllers/` | All HTTP endpoints | -> `Controllers/AGENTS.md` |
| `Middleware/` | Request pipeline: correlation ID, version check, meta-hash, session validation, global exception handler |
| `Program.cs` | ASP.NET Core entry point: DI registrations, middleware order |
| `ProjectLinkConfiguration.cs` | Strict env/config loader for deploy/runtime values |
| `MockAuthenticationHandler.cs` | Development/mock auth scheme for local guest tokens |
| `ShortSourceContextEnricher.cs` | Serilog enricher that shortens SourceContext values to class names |
| `appsettings.Development.json` | Development Serilog level overrides |
| `appsettings.Production.json` | Production Serilog level and WARN+ file sink settings |

## Rules
- `userId` extracted from authenticated claims in every authenticated controller; never from request body.
- Unauthenticated endpoints: `GET /health`, `GET /api/bootstrap/config`, `GET /api/events/season`, `POST /api/auth/guest`, `POST /api/auth/refresh`, `POST /api/auth/google`, `POST /api/auth/logout`.
- Middleware order in `Program.cs`: CorrelationId -> SerilogRequestLogging -> GlobalException -> HTTPS -> Auth -> Authorization -> RateLimit -> VersionCheck -> MetaHash -> SessionValidation -> Controllers.
