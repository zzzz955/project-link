# ProjectLink.Application/Bootstrap

## Files
| file | class | role |
|------|-------|------|
| `BootstrapService.cs` | `BootstrapService` | Returns app version, server time, meta hash, maintenance flag |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `BootstrapService.Get` | method | reads from IConfiguration; no DB access; safe for unauthenticated callers |

## Cross-refs
- Consumed by: server `API.Controllers.BootstrapController` → `GET /api/bootstrap/config`

## Rules
- IConfiguration keys: `Bootstrap:ClientVersion`, `Bootstrap:RequiredClientVersion`, `Bootstrap:ProtocolVersion`, `Bootstrap:MetaHash`, `Bootstrap:Maintenance`, `Bootstrap:MaintenanceMessage`
- No authentication required on this endpoint
