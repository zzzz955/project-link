# shared/design

Game design documentation and rule specs.

## Nav

| path | role |
|------|------|
| `ingame/` | InGame rules and board behavior specs |

## Rules

- Use ASCII only.
- Keep specs data-driven and aligned with `shared/datas/**` source CSVs.
- Update child `AGENTS.md` files when adding or changing documented files.
- Every directory with `AGENTS.md` must also have `CLAUDE.md` containing exactly `@AGENTS.md`.
