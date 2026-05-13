# Utils — Shared stateless helpers

## Files
| file | class | role |
|---|---|---|
| `CsvLoader.cs` | `CsvLoader` | Loads typed arrays from Resources CSV via reflection |
| `GridUtils.cs` | `GridUtils` | Grid ↔ world coordinate conversion |
| `ColorPalette.cs` | `ColorPalette` | nodeGroupId → Unity Color; data-driven via Init() |

## Symbols
| symbol | kind | note |
|---|---|---|
| `CsvLoader.Load<T>(string)` | method | static; Resources path → T[]; row 1 = field names mapped to public fields |
| `CsvLoader.ConvertValue(string,Type)` | method | converts CSV cells to primitive field values; empty string fields stay `""` |
| `GridUtils.CellToWorld(int,int,int,int,float)` | method | static; (x,y,width,height,cellSize) → centered Vector2; board origin at world (0,0) |
| `ColorPalette.Init(Dictionary<int,Color>)` | method | static; populates color map from StageData.NodeColors; must be called before Get() |
| `ColorPalette.Get(int)` | method | static; nodeGroupId → Color; returns magenta for unknown IDs |

## Cross-refs
- Consumed by: client `Data.StageLoader` (CsvLoader.Load for IngameStage/IngameNodeColors), client `Core.LocalizationManager` (CsvLoader.Load for Clientstring)
- Consumed by: client `InGame.Board.BoardView`, `InGame.Path.PathView` (GridUtils.CellToWorld)
- Consumed by: client `Core.InGameController`, `InGame.Board.CellView` (ColorPalette.Get)
- Depends on: (pure utility layer — no game data dependencies)

## Rules
- All classes are static utility; no MonoBehaviour, no state (ColorPalette holds initialized dictionary)
- CsvLoader uses reflection — field names in T must exactly match CSV header row
- GridUtils centers the board at world origin; adjust camera with FitCameraToBoard in InGameController
- ColorPalette.Init() must be called by InGameController before any CellView.Refresh()
