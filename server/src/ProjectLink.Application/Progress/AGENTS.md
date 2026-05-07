# ProjectLink.Application/Progress

## Files
| file | class | role |
|------|-------|------|
| `GetProgressQuery.cs` | `GetProgressQueryHandler` | Batch stage-progress query — returns unlock status computed from cleared stage IDs |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `GetProgressQueryHandler.HandleAsync` | method | Reads all `StageProgress` records, computes `IsUnlocked` (`stageId == 1 || clearedIds.Contains(stageId - 1)`), returns `ProgressResponse` DTO |

## Cross-refs
- Depends on: `IProgressRepository`, `IStaticDataService`
- Consumed by: `API.Controllers.ProgressController` → `POST /api/progress/batch`

## Rules
- Endpoint is a query (POST with body `{ stageIds }`) not an upsert — `UpsertProgressCommand` is removed
- `IsUnlocked` is computed server-side; client must not derive unlock state locally
