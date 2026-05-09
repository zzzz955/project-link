# tools - Automation Pipeline

## Nav
| file | role |
|------|------|
| `config-loader.js` | Loads `template.ini` + `.env`, exports merged config |
| `gen-data.js` | `shared/datas/**/*.csv` -> `*/generated/data/**/*.csv` |
| `gen-orm.js` | `server/db/schema.json` -> DB CREATE/ALTER TABLE + migration SQL |
| `gen-all.bat` / `gen-all.sh` | Runs all gen steps in order |
| `gen-data.bat` | Runs gen-data only |
| `gen-orm.bat` | Runs gen-orm only |
| `run-gen-step.ps1` | Streams gen batch step output to console + `tools/logs/*.log` |
| `start-stage-editor.bat` | Starts stage editor dev server (UI + API) with CRUD console logs |
| `stage-tool/` | Local TypeScript web tool for stage CRUD, validation, and visualization |

## Rules
- ALL scripts read config from `config-loader.js` - never hardcode paths
- `_` prefix files/dirs are skipped by all gen tools
- Errors report as: `[tool] ERROR: <file>\n  <location>: <message>`
- On any error - print all errors, then `process.exit(1)`
- gen-orm default: `dry_run=true` (SQL file only) - set `false` in `template.ini` to execute
- gen-orm auto-installs the DB driver package when physical DB sync needs it and it is missing

## Serena
FIND: config values - `config-loader.js` exports
FIND: CSV parsing logic - `gen-data.js` `parseCSV()`
FIND: DB schema sync - `gen-orm.js` `main()`

## Adding a new gen tool
1. Create `tools/gen-[name].js` - use `config-loader.js` for all config
2. Add `"gen:[name]": "node tools/gen-[name].js"` to root `package.json` scripts
3. Add step to `gen-all.bat` and `gen-all.sh`
4. Update this Nav section
