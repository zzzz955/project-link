# InGame/Input — Touch input, longpress detection, cell snapping, erase mode

## Files
| file | class | role |
|---|---|---|
| `TouchInputHandler.cs` | `TouchInputHandler` | Unified drag + longpress detector via New Input System |
| `InputSnapper.cs` | `InputSnapper` | World position → nearest in-bounds grid cell |
| `EraseController.cs` | `EraseController` | Longpress-triggered path erase with circular gauge fill |

## Symbols
| symbol | kind | note |
|---|---|---|
| `TouchInputHandler.OnDragStart` | event | `Action<Vector2>` — fires at _pressStartWorld when move confirmed |
| `TouchInputHandler.OnDragMove` | event | `Action<Vector2>` — fires every frame while dragging |
| `TouchInputHandler.OnDragEnd` | event | `Action<Vector2>` — fires on release (non-longpress path) |
| `TouchInputHandler.OnLongPressStart` | event | `Action<Vector2>` — fires after 0.7 s stationary hold |
| `TouchInputHandler.OnLongPressCanceled` | event | `Action` — fires on release after longpress confirmed |
| `InputSnapper.Snap(Vector2,Board,float)` | method | static; clamps to board bounds; returns Cell reference |
| `EraseController.Init(...)` | method | wires to TouchInputHandler events; call once after board created |
| `EraseController.Cancel()` | method | public; stops gauge coroutine, hides CircularGauge, → Idle |

## Rules
- **OnDragStart is DEFERRED**: fires only when `moved > _longPressMoveLimit` (0.15 world units, Inspector-configurable)
  Reason: immediate fire caused TryStartPath to clear completed paths before longpress could trigger erase
- Longpress threshold: `_longPressThreshold = 0.7 s` (Inspector-configurable)
- Once longpress fires, movement events are suppressed for that press cycle
- EraseController.Cancel() is called externally by InGameController.HandleTimeUp when Erasing at timeout
- Erase requires: cell.IsNode == true AND path.IsComplete == true AND FSM.Current == Idle
