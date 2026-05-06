# InGame/Board — Grid data model and cell rendering

## Files
| file | class | role |
|---|---|---|
| `Cell.cs` | `Cell` | Single grid cell: state enum + colorId |
| `Board.cs` | `Board` | 2D grid; cell access, path operations |
| `BoardView.cs` | `BoardView` | MonoBehaviour; creates and refreshes all CellViews |
| `CellView.cs` | `CellView` | MonoBehaviour; single cell sprite renderer |

## Symbols
| symbol | kind | note |
|---|---|---|
| `Cell.State` | prop | `CellState`: Empty \| Node \| Path |
| `Cell.ColorId` | prop | 0 when Empty |
| `Cell.IsEmpty` / `IsNode` / `IsPath` | props | convenience booleans |
| `Cell.SetNode(int)` | method | transition → Node with colorId |
| `Cell.SetPath(int)` | method | transition → Path with colorId |
| `Cell.Clear()` | method | transition → Empty, colorId=0 |
| `Board.Width` / `Height` | props | grid dimensions |
| `Board.ColorIds` | prop | `IReadOnlyCollection<int>` — all node color IDs |
| `Board.GetCell(int,int)` | method | direct cell access; no bounds check inside |
| `Board.IsInBounds(int,int)` | method | guard before GetCell |
| `Board.GetAdjacentCells(int,int)` | method | yields up to 4 orthogonal neighbors |
| `Board.SetPath(int,int,int)` | method | marks cell as Path for colorId |
| `Board.ClearPathCells(int)` | method | resets all Path cells of colorId to Empty |
| `BoardView.Init(Board,float)` | method | instantiates CellView grid; call once after Board created |
| `BoardView.Refresh()` | method | syncs all CellView colors from current Board state |
| `CellView.Init(Cell,float)` | method | binds to cell reference, sets sprite size |
| `CellView.Refresh()` | method | repaints sprite: Node=full color, Path=×0.65, Empty=gray(0.15) |

## Rules
- `Board` is pure data — no MonoBehaviour, no Unity types
- Sorting layer order: Board (background) → Path → Node
- Color mapping enforced in CellView.Refresh only; Board holds no color data
