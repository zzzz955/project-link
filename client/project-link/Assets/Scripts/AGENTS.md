# Scripts — root-level shared runtime scripts

## Files
| file | class | role |
|---|---|---|
| `GeneratedUIMarker.cs` | `GeneratedUIMarker` | MonoBehaviour marker on every builder-created GO; persists stableId across scene saves |

## Symbols
| symbol | kind | note |
|---|---|---|
| `GeneratedUIMarker.stableId` | field | deterministic hash string; set by `ProjectLinkUIBuilder.AssignStableIds` after build |
| `GeneratedUIMarker.ComputeId(target,path)` | method | MD5(target + \x01 + path) → 8-char hex; deterministic per-build |

## Cross-refs
- Consumed by: `Editor.ProjectLinkUIBuilder.AssignStableIds`, `Editor.ProjectLinkUIOverrideCapture.WalkGO`, `Editor.ProjectLinkUIOverrideApply.FindByStableId`

## Rules
- Included in player build (empty at runtime — no performance impact).
- stableId is stable only when hierarchy path is unchanged; path changes yield a new ID (orphaned entries get path-fallback in apply).
