# shared/design/ingame

InGame board, path, node, and stage rule documentation.

## Files

| file | class | role |
|------|-------|------|
| `game_rules.md` | DesignSpec | Defines InGame board data, node groups, drawing, path ownership, clear condition, and large-board camera requirements |

## Symbols

| symbol | kind | note |
|--------|------|------|
| `StageData.nodeMap` | data-layer | Row-major node group placement layer |
| `StageData.cellMap` | data-layer | Row-major obstacle, gimmick, and cell metadata layer |
| `NodeGroup.nodeGroupId` | rule | Valid IDs are `1..20`; colors come from `ingame_node_colors.csv` |
| `PathModel.IncompletePath` | rule | Persists after input ends and resumes only from the tail |
| `PathModel.OverwriteErase` | rule | Drawing over a drawable filled cell clears previous ownership and paints the active group |
| `ClearCondition.ValidPairs` | rule | Stage clears only when every node in every group remains connected as valid pairs |
| `Camera.LargeBoard` | requirement | Large boards require zoom and pan with transformed hit testing |

## Rules

- Use ASCII only.
- Keep runtime rules aligned with stage editor validation and solver validation.
- Do not document generated files here.
- When `game_rules.md` changes, update affected symbol notes in this file.
