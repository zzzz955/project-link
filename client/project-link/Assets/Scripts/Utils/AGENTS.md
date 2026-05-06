# Utils — Shared stateless helpers

## Files
| file | class | role |
|---|---|---|
| `CsvLoader.cs` | `CsvLoader` | Loads typed arrays from Resources CSV via reflection |
| `GridUtils.cs` | `GridUtils` | Grid ↔ world coordinate conversion |
| `ColorPalette.cs` | `ColorPalette` | colorId (1–6) → Unity Color lookup |

## Symbols
| symbol | kind | note |
|---|---|---|
| `CsvLoader.Load<T>(string)` | method | static; Resources path → T[]; row 1 = field names mapped to public fields |
| `GridUtils.CellToWorld(int,int,int,int,float)` | method | static; (x,y,width,height,cellSize) → centered Vector2; board origin at world (0,0) |
| `ColorPalette.Get(int)` | method | static; colorId 1–6; returns white for unknown IDs |

## Rules
- All classes are static utility; no MonoBehaviour, no state
- CsvLoader uses reflection — field names in T must exactly match CSV header row
- GridUtils centers the board at world origin; adjust camera with FitCameraToBoard in InGameController
