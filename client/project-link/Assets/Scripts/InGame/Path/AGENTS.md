# InGame/Path — Path drawing, validation, and rendering

## Files
| file | class | role |
|---|---|---|
| `PathModel.cs` | `PathModel` | Ordered cell list + completion state for one group |
| `PathDrawer.cs` | `PathDrawer` | Drawing orchestrator; manages List<PathModel> per groupId |
| `PathValidator.cs` | `PathValidator` | Static movement and completion rule checks |
| `PathView.cs` | `PathView` | MonoBehaviour; LineRenderer for one path |

## Symbols
| symbol | kind | note |
|---|---|---|
| `PathModel.ColorId` | prop | groupId this path belongs to |
| `PathModel.Cells` | prop | `IReadOnlyList<Cell>` — ordered from start node |
| `PathModel.IsComplete` | prop | true when both same-group nodes are connected |
| `PathModel.AddCell(Cell)` | method | appends cell; sets IsComplete if cell is destination node (checks NodeGroupId) |
| `PathModel.TruncateTo(int,int)` | method | removes all cells after (x,y); backtrack support |
| `PathModel.TruncateBefore(int,int)` | method | removes cell at (x,y) and everything after; overwrite erase support |
| `PathModel.IndexOf(int,int)` | method | returns index of cell at (x,y); -1 if not found |
| `PathModel.Contains(int,int)` | method | O(1) via internal HashSet |
| `PathModel.Clear()` | method | empties Cells, resets IsComplete |
| `PathDrawer.ActivePath` | prop | currently-drawing PathModel; null when Idle |
| `PathDrawer.TryStartPath(Cell)` | method | Node → start fresh (clears paths ending at node) OR tail of incomplete path → resume; → Drawing |
| `PathDrawer.ProcessCell(Cell)` | method | extends or truncates active path; overwrite removes other group's tail; diagonal auto-split via L-step |
| `PathDrawer.EndPath()` | method | complete→Idle/Completed; incomplete→persist on board (do NOT clear) |
| `PathDrawer.GetPaths(int)` | method | returns IReadOnlyList<PathModel> for groupId; empty if none |
| `PathDrawer.GetPath(int)` | method | compat; returns first path for groupId; null if none |
| `PathDrawer.AllPaths()` | method | enumerates all (groupId, PathModel) pairs across all groups |
| `PathValidator.IsAdjacent(Cell,Cell)` | method | 4-way orthogonal only; no diagonal |
| `PathValidator.CanMoveTo(Cell,int)` | method | true if cell.IsDrawable AND not a node from another group |
| `PathValidator.IsCleared(Board,Dictionary)` | method | primary: all groups have every node as endpoint of a complete path |
| `PathValidator.IsGroupConnected(IReadOnlyList<Cell>,IReadOnlyList<PathModel>)` | method | true when every node in group is endpoint of at least one complete path |
| `PathValidator.IsCleared(IReadOnlyCollection<int>,...)` | method | compat signature; kept for remaining references |
| `PathView.Init(PathModel,int,int,float)` | method | binds LineRenderer to PathModel; boardWidth/Height/cellSize |
| `PathView.Refresh()` | method | rebuilds LineRenderer positions from PathModel.Cells |

## Rules
- Multi-path per group: `_paths` is `Dictionary<int, List<PathModel>>`; each group can have multiple paths
- `TryStartPath` on a Node clears all existing paths that start or end at that node, then creates a new one
- `TryStartPath` on a non-node path tail resumes the incomplete path (no new PathModel created)
- Incomplete paths persist on board after `EndPath` — NOT cleared
- Overwrite: drawing over another group's cells truncates that group's path from the overwritten cell onward
- `UpdateIsComplete` uses `NodeGroupId` (not `ColorId`) for the node check
- PathView renders active color in real-time via Refresh on every DragMove; binds to PathModel directly
