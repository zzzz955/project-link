# docs

## Nav
| path | role | link |
|------|------|------|
| `refs/` | Token-efficient references for external platform dependencies. | -> `refs/AGENTS.md` |
| `ui-design.md` | Project Link UI/UX design source for scenes, popups, and event-driven behavior. | |
| `ui-skin-keys.md` | UIButtonSkin button/image slot key registry. | |

## Cross-repository refs
- Project Link consumes platform-owned features through public contracts, OpenAPI operations, JWKS/OIDC metadata, and platform architecture docs.
- Platform implementation internals are not game contracts.
- For platform auth, read `refs/platform-auth.md` first, then `platform:docs/refs/auth.md`.
- Use `repo:path`, `repo:SymbolName`, and `service:METHOD /route` notation.
- Game docs list `Depends on:` platform refs; platform docs list `Consumed by:` game refs.
- Do not copy platform security rules into gameplay docs unless the local behavior depends on them; link the platform source of truth instead.

## Rules
- Keep docs focused on project decisions and external dependency refs.
- Do not duplicate platform source-of-truth docs; link them with `platform:path`.
- New subdirectories require `AGENTS.md`, `CLAUDE.md`, and this Nav update.
