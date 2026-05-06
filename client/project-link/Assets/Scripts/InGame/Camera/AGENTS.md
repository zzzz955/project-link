# InGame/Camera — Large board camera zoom and pan

## Files
| file | class | role |
|---|---|---|
| `BoardCameraController.cs` | `BoardCameraController` | MonoBehaviour; pinch-zoom + 2-finger pan for large boards |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `BoardCameraController.Init(Board,float)` | method | stores board bounds and min/max zoom; call after FitCameraToBoard sets initial size |

## Rules
- Added to Camera.main's GameObject by InGameController.Start
- 1-finger input reserved for path drawing; camera only reacts to 2-finger gestures
- MinZoom = initial orthographicSize after FitCameraToBoard; MaxZoom = MinZoom × 3
- EnhancedTouchSupport must be enabled; handled in OnEnable/OnDisable
