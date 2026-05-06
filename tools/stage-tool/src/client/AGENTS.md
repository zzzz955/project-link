# tools/stage-tool/src/client - Stage Tool Frontend

## Files
| file | class | role |
|------|-------|------|
| `main.tsx` | `App` | React UI for stage CRUD, grid editing, palettes, and validation display |
| `styles.css` | CSS | Stage tool layout, palette, board, and responsive styles |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `App` | component | Loads stage data/node colors and renders the editor |
| `stagePayload` | function | Converts grid state to API payload arrays |
| `normalizeStage` | function | Normalizes API or draft data into UI state |

## Rules
- Keep API calls under `/api`; Vite proxies them to the local server in development.
- Render node group colors from `/api/node-colors`; fallback colors are UI-only.
- Preserve overlapping cells when resizing a board.
