# Resources - Runtime-loadable Unity assets

## Nav
| path | role | link |
|---|---|---|
| `Data/` | Generated runtime CSV data | `Data/AGENTS.md` |
| `UI/` | UI reference sprites and AssetResource sprite sheets | `UI/AGENTS.md` |
| `Prefabs/` | Runtime-loadable prefabs | `Prefabs/AGENTS.md` |

## Rules
- Assets in this tree are loaded through Unity `Resources`.
- Do not edit generated `Data/` outputs directly; edit `shared/datas/**/*.csv` and run `tools/gen-data.bat`.
