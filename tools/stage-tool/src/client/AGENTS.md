# tools/stage-tool/src/client - Stage Tool Frontend

## Files
| file | class | role |
|------|-------|------|
| `main.tsx` | `App` | React UI for stage CRUD, generator settings, drag grid editing, palettes, and validation display |
| `styles.css` | CSS | Stage tool layout, palette, board, and responsive styles |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `App` | component | Loads stage data/node colors and renders the editor |
| `stagePayload` | function | Converts grid state to API payload arrays |
| `normalizeStage` | function | Normalizes API or draft data into UI state |
| `normalizeGeneratedStage` | function | Extracts generated board and seed metadata from `/api/stages/generate` responses |
| `normalizeValidationErrors` | function | Normalizes backend validation and solver issue payloads for display |
| `clampDifficulty` | function | Constrains editor/generator difficulty to 1..5 |
| `clampNodeCount` | function | Constrains generator node group count to `1..20` |
| `appendDragCells` | function | Builds row/column drag preview and erase paths between pointer-entered cells |
| `App.editorDefaults` | state | Defaults fetched from `/api/defaults`; used by Add button to initialize new stage fields |
| `App.newStage` | method | Creates new stage with stageId = maxId+1 and values from `editorDefaults` |

## Rules
- Keep API calls under `/api`; Vite proxies them to the local server in development.
- Render node group colors from `/api/node-colors`; fallback colors are UI-only.
- Preserve overlapping cells when resizing a board.
- Generated boards load into the editor draft state only; save remains an explicit user action.
- Left-drag in node mode previews a light path and places endpoints; right-drag clears cells.
