# tools/stage-tool/src/server - Stage Tool Server

## Files
| file | class | role |
|------|-------|------|
| `csv.ts` | - | CSV parser/writer preserving metadata rows |
| `http.ts` | - | Local HTTP routing and JSON responses |
| `index.ts` | - | Server entrypoint |
| `paths.ts` | - | Project root and source CSV path resolution |
| `stageRepository.ts` | `StageRepository` | CSV-backed stage CRUD and node color reads |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `createServer` | function | Creates the local HTTP API server |
| `StageRepository.createStage` | method | Adds only the next contiguous `stageId` |
| `StageRepository.updateStage` | method | Updates only existing stages |
| `StageRepository.deleteStage` | method | Deletes only the last stage |
| `StageRepository.validateStage` | method | Runs structural validation without writing |

## Rules
- Writes must be atomic temp-file renames.
- Preserve CSV metadata rows exactly when saving.
- Return validation failures as JSON `issues`.
