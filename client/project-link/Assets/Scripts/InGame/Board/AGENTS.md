# InGame/Board — Grid data model and cell rendering

## Files
| file | class | role |
|---|---|---|
| `Cell.cs` | `Cell` | Single grid cell: authored type (CellType) + runtime path ownership |
| `Board.cs` | `Board` | 2D grid; cell access, path claim/release, group node lookup |
| `BoardView.cs` | `BoardView` | MonoBehaviour; creates and refreshes all CellViews |
| `CellView.cs` | `CellView` | MonoBehaviour; single cell sprite renderer |

## Symbols
| symbol | kind | note |
|---|---|---|
| `CellType` | enum | `Empty \| Node \| Obstacle \| Gimmick` |
| `Cell.Type` | prop | `CellType`; authored cell type |
| `Cell.NodeGroupId` | prop | int; group id when Node, else 0 |
| `Cell.PathOwner` | prop | int; groupId of path owner, 0 if none |
| `Cell.IsNode` / `IsObstacle` / `IsGimmick` | props | type convenience booleans |
| `Cell.IsDrawable` | prop | true when Empty or Node (path can be drawn here) |
| `Cell.HasPath` | prop | true when PathOwner > 0 |
| `Cell.IsEmpty` / `IsPath` / `ColorId` | props | compat aliases; ColorId = NodeGroupId when node, else PathOwner |
| `Cell.SetNode(int)` | method | transition → Node with groupId |
| `Cell.SetObstacle()` | method | transition → Obstacle |
| `Cell.SetGimmick()` | method | transition → Gimmick |
| `Cell.ClaimPath(int)` | method | sets PathOwner to groupId |
| `Cell.ReleasePath()` | method | clears PathOwner (sets to 0) |
| `Cell.Clear()` | method | compat alias for ReleasePath |
| `Board.Width` / `Height` | props | grid dimensions |
| `Board.GroupIds` | prop | `IReadOnlyCollection<int>` — all node group IDs |
| `Board.ColorIds` | prop | compat alias for GroupIds |
| `Board.GetCell(int,int)` | method | direct cell access; no bounds check inside |
| `Board.IsInBounds(int,int)` | method | guard before GetCell |
| `Board.GetAdjacentCells(int,int)` | method | yields up to 4 orthogonal neighbors |
| `Board.GetGroupNodes(int)` | method | returns `IReadOnlyList<Cell>` of node cells for a groupId |
| `Board.ClaimPath(int,int,int)` | method | claims path on cell (x,y) for groupId |
| `Board.ReleasePath(int,int)` | method | releases path on cell (x,y) |
| `Board.ClearGroupPaths(int)` | method | releases all path cells owned by groupId |
| `Board.SetPath(int,int,int)` | method | compat alias for ClaimPath |
| `Board.ClearPathCells(int)` | method | compat alias for ClearGroupPaths |
| `BoardView.Init(Board,float)` | method | instantiates CellView grid; call once after Board created |
| `BoardView.Refresh()` | method | syncs all CellView colors from current Board state |
| `CellView.Init(Cell,float)` | method | binds to cell reference, sets sprite size |
| `CellView.Refresh()` | method | repaints sprite: Obstacle=dark gray, Gimmick=cyan, Node=full color, Path=×0.65, Empty=gray(0.15) |

## Rules
- `Board` is pure data — no MonoBehaviour, no Unity types
- Sorting layer order: Board (background) → Path → Node
- Color mapping enforced in CellView.Refresh only; Board holds no color data
- `CellState` enum removed; replaced by `CellType` with Obstacle/Gimmick support
