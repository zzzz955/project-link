# ProjectLink.Application/Settings

## Files
| file | class | role |
|------|-------|------|
| `PlayerSettingsService.cs` | `PlayerSettingsService` | Server-synced player preferences (partial update supported) |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `PlayerSettingsService.GetAsync` | method | Returns defaults if no row exists (never 404) |
| `PlayerSettingsService.UpdateAsync` | method | Partial update — only non-null request fields are applied |

## Cross-refs
- Consumed by: `API.Controllers.PlayerSettingsController` → `GET /api/settings`, `PATCH /api/settings`
- Depends on: `IPlayerSettingsRepository`

## Rules
- Settings sync is optional from the client perspective; local PlayerPrefs remains valid
- `PATCH` semantics: only fields present in request body overwrite existing values
