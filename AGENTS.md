# Game Development Template

## Nav
| path | role |
|------|------|
| `shared/` | Shared C# contracts, shared types, game meta data | -> `shared/AGENTS.md` |
| `tools/` | Automation pipeline (gen-data, gen-packets, gen-orm) | -> `tools/AGENTS.md` |
| `client/` | Client app - stack defined by user | -> `client/AGENTS.md` |
| `server/` | Server app + DB schema - stack defined by user | -> `server/AGENTS.md` |
| `TODO-List/` | Release TODO tracker - per-area task lists + progress summary | -> `TODO-List/AGENTS.md` |

## Pipeline
```
shared/datas/**/*.csv  -> gen:data -> {client,server}/generated/data/**/*.csv
server/db/schema.json  -> gen:orm  -> DB CREATE/ALTER TABLE (+ migration SQL)
shared/contracts/*.cs  -> manual   -> server ProjectReference + Unity Assets/Scripts/Generated/Contracts/
```
CMD: `tools/gen-all.bat` | `tools/gen-data.bat` | `tools/gen-orm.bat` | `npm run gen:all`

## Rules
- NEVER edit `*/generated/*` — edit source, re-run gen
- NEVER commit `.env` — use `.env.example`
- NEVER store secrets in `template.ini` — secrets go in `.env`
- CONFIG priority: `.env` > `template.ini` > hardcoded defaults
- `_` prefix files/dirs are skipped by all gen tools (examples, drafts)

## Documentation Convention
Every directory containing client/server/design/data/packet content must be documented.
This convention is enforced by AI agents; violations should be fixed before committing.

**AGENTS.md** — AI-agent instructions, written in English, token-efficient:
- Leaf dirs: `## Files` table (file→class→role) + `## Symbols` table (symbol→kind→note) + `## Rules`
- Parent/nav dirs: `## Nav` table (path→role→link) + minimal `## Rules`
- Symbols use `ClassName.MemberName` notation — directly grep/Serena-searchable
- When new files are added to a directory → update that directory's `## Files` and `## Symbols`
- When a new subdirectory is created → create its AGENTS.md + update parent's `## Nav`
- When existing logic changes → update the affected symbol entries in AGENTS.md

**CLAUDE.md** — Claude Code context loader (wrapper only):
- Every directory that has an `AGENTS.md` must also have a `CLAUDE.md`
- Contents must be exactly one line: `@AGENTS.md`
- Never write instructions directly into `CLAUDE.md` — use `AGENTS.md` instead

## Agent Context Convention
- `AGENTS.md` is the single source of truth for AI agent instructions
- Edit only `AGENTS.md`; never edit `CLAUDE.md` directly
- `CLAUDE.md` must remain a Claude Code compatibility wrapper: contents = `@AGENTS.md`

## Formats
FILE (data):      `[domain]_[table].csv`         e.g. `characters_base.csv`
FILE (contracts): `[Domain]Requests.cs`          e.g. `StageRequests.cs`
FILE (contracts): `[Domain]Responses.cs`         e.g. `StageResponses.cs`
FILE (db):        `schema.json` (single file)
FILE (gen):       auto-named from source filename

## Output
- No narration before tool calls - execute immediately, no "Let me read X" preamble
- Silent on success path - only surface errors or blockers mid-execution
- Autonomous decisions (Priority, Size, etc.): state value only, omit reasoning
- Final report: compact table or key-value pairs, no prose
- No trailing summary - do not recap what was just completed
