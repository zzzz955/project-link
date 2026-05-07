# tools/stage-tool/src/server - Stage Tool Server

## Files
| file | class | role |
|------|-------|------|
| `csv.ts` | - | CSV parser/writer preserving metadata rows |
| `http.ts` | - | Local HTTP routing and JSON responses |
| `index.ts` | - | Server entrypoint |
| `paths.ts` | - | Project root and source CSV path resolution |
| `stageRepository.ts` | `StageRepository` | CSV-backed stage CRUD and node color reads |
| `validateStages.ts` | - | CLI validation entrypoint for CI |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `createServer` | function | Creates the local HTTP API server; accepts `EditorDefaults` as second arg |
| `GET /api/defaults` | route | Returns editor defaults read from `[stage-editor]` section of `template.ini` |
| `POST /api/stages/generate` | route | Returns an unsaved generated payload from size, difficulty, node count, seed, and validation status |
| `StageRepository.createStage` | method | Adds only the next contiguous `stageId`; logs `[CRUD] CREATE` to stdout |
| `StageRepository.updateStage` | method | Updates only existing stages; logs `[CRUD] UPDATE` to stdout |
| `StageRepository.deleteStage` | method | Deletes only the last stage; logs `[CRUD] DELETE` to stdout |
| `StageRepository.validateStage` | method | Runs structural validation without writing |
| `validateStages.ts.main` | function | Validates all source stage rows and exits nonzero on failure |
| `resolveEditorDefaults` | function | Reads `[stage-editor]` from `template.ini`; returns `EditorDefaults` with width/height/timeLimit/difficulty |

## Rules
- Writes must be atomic temp-file renames.
- Preserve CSV metadata rows exactly when saving.
- Return validation failures as JSON `issues`.
