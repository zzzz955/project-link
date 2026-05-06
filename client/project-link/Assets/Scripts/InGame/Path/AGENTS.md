# InGame/Path — Path drawing, validation, and rendering

## Files
| file | class | role |
|---|---|---|
| `PathModel.cs` | `PathModel` | Ordered cell list + completion state for one color |
| `PathDrawer.cs` | `PathDrawer` | Drawing orchestrator; manages PathModel per colorId |
| `PathValidator.cs` | `PathValidator` | Static movement and completion rule checks |
| `PathView.cs` | `PathView` | MonoBehaviour; LineRenderer for one path |

## Symbols
| symbol | kind | note |
|---|---|---|
| `PathModel.ColorId` | prop | color this path belongs to |
| `PathModel.Cells` | prop | `IReadOnlyList<Cell>` — ordered from start node |
| `PathModel.IsComplete` | prop | true when both same-color nodes are connected |
| `PathModel.AddCell(Cell)` | method | appends cell; sets IsComplete if cell is destination node |
| `PathModel.TruncateTo(int,int)` | method | removes all cells after (x,y); backtrack support |
| `PathModel.Contains(int,int)` | method | O(1) via internal HashSet |
| `PathModel.Clear()` | method | empties Cells, resets IsComplete |
| `PathDrawer.TryStartPath(Cell)` | method | requires Idle state + IsNode; clears existing path; → Drawing |
| `PathDrawer.ProcessCell(Cell)` | method | extends or truncates active path; diagonal auto-split via L-step |
| `PathDrawer.EndPath()` | method | complete→Idle/Completed; incomplete→clear+Idle |
| `PathDrawer.GetPath(int)` | method | returns PathModel for colorId; null if none started |
| `PathValidator.CanMoveTo(Cell,int)` | method | true if Empty or same-color destination node |
| `PathValidator.IsAdjacent(Cell,Cell)` | method | 4-way orthogonal only; no diagonal |
| `PathValidator.IsCleared(IReadOnlyCollection<int>,...)` | method | all colorIds have IsComplete paths |
| `PathView.Init(PathModel,int,int,float)` | method | binds LineRenderer to PathModel; boardWidth/Height/cellSize |
| `PathView.Refresh()` | method | rebuilds LineRenderer positions from PathModel.Cells |

## Rules
- One PathModel per colorId; `TryStartPath` clears existing before creating new (game Rule 5)
- Diagonal input → PathDrawer.ProcessCell inserts one L-shaped intermediate cell
- `EndPath` must always be called on DragEnd, even if drawing never started (safe: no-op when _activePath==null)
- PathView renders active color in real-time via Refresh on every DragMove
