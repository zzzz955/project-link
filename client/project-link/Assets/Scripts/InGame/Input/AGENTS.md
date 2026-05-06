# InGame/Input — Touch input, longpress detection, cell snapping

## Files
| file | class | role |
|---|---|---|
| `TouchInputHandler.cs` | `TouchInputHandler` | Unified drag + longpress detector via New Input System |
| `InputSnapper.cs` | `InputSnapper` | World position → nearest in-bounds grid cell |
| `EraseController.cs` | `EraseController` | Empty MonoBehaviour stub; kept for Unity scene serialization compat |

## Symbols
| symbol | kind | note |
|---|---|---|
| `TouchInputHandler.OnDragStart` | event | `Action<Vector2>` — fires at _pressStartWorld when move confirmed |
| `TouchInputHandler.OnDragMove` | event | `Action<Vector2>` — fires every frame while dragging |
| `TouchInputHandler.OnDragEnd` | event | `Action<Vector2>` — fires on release (non-longpress path) |
| `TouchInputHandler.OnLongPressStart` | event | `Action<Vector2>` — fires after 0.7 s stationary hold (unsubscribed; kept for compat) |
| `TouchInputHandler.OnLongPressCanceled` | event | `Action` — fires on release after longpress confirmed (unsubscribed; kept for compat) |
| `InputSnapper.Snap(Vector2,Board,float)` | method | static; clamps to board bounds; returns Cell reference |

## Rules
- **OnDragStart is DEFERRED**: fires only when `moved > _longPressMoveLimit` (0.15 world units, Inspector-configurable)
- Longpress threshold: `_longPressThreshold = 0.7 s` (Inspector-configurable)
- Once longpress fires, movement events are suppressed for that press cycle
- EraseController is a gutted stub — longpress erase removed; overwrite erase is handled by PathDrawer.ProcessCell
- OnLongPressStart / OnLongPressCanceled events still exist on TouchInputHandler but are not subscribed by anyone
