# ProjectLink.Infrastructure/Data

## Files
| file | class | role |
|------|-------|------|
| `StaticDataService.cs` | `StaticDataService` | Singleton — loads ingame CSV files at startup into memory dictionaries |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `StaticDataService.GetStage` | method | O(1) lookup by stageId; returns null if not found |
| `StaticDataService.GetItem` | method | O(1) lookup by itemId; returns null if not found |
| `StaticDataService.GetAllStages` | method | used by ranking cold-start rebuild |

## Rules
- Loads from `AppContext.BaseDirectory/generated/data/ingame/` — files must be present at startup
- Missing CSV → logs warning + returns empty dictionary (does not crash)
- stageMeta column parsing handles embedded commas via `cols[8..^1]` join
- Run `npm run gen:data` before first server start after adding new CSV files
