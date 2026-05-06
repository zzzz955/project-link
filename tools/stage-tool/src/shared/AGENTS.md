# tools/stage-tool/src/shared - Stage Tool Shared Domain

## Files
| file | class | role |
|------|-------|------|
| `codec.ts` | - | Base36 fixed-width 2-char map encode/decode |
| `generator.ts` | - | Deterministic solvable stage payload generation |
| `index.ts` | - | Shared exports |
| `solver.ts` | - | Deterministic BFS/backtracking pair routing validation |
| `stage.ts` | `ValidationError` | Stage/node color types and validation error class |
| `validation.ts` | - | Stage row, sequence, CRUD, and solver validation |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `encodeFixedBase36` | function | Encodes row-major map values to 2-char base36 tokens |
| `decodeFixedBase36` | function | Decodes `nodeMap`/`cellMap` strings |
| `generateStagePayload` | function | Builds deterministic generated payloads for width, height, difficulty, and seed |
| `validateStageSolvable` | function | Pairs node groups with BFS/backtracking and marks occupied route cells |
| `normalizeAndValidateStage` | function | Validates dimensions, maps, collisions, node parity, and solver reachability |
| `validateCreateStageId` | function | Allows only `maxStageId + 1` |
| `validateUpdateStageId` | function | Allows only existing stage IDs |
| `validateDeleteStageId` | function | Allows only the last stage ID |

## Rules
- Node groups are `1..20`; `0` means empty.
- Cell map codes are `0..1295`; `0` empty, `1` obstacle, `2+` gimmicks.
- Difficulty is `1..5`.
- Validation here must be reusable by server, frontend, and CI scripts.
