# client - Unity 6 (URP 2D)

## Stack
Unity 6000.4.5f1 | URP 2D (17.4.0) | C# | New Input System (1.19.0)

## Nav
| path | role |
|---|---|
| `project-link/Assets/Scripts/Core/` | App lifecycle, singletons, FSM, popups -> `Core/AGENTS.md` |
| `project-link/Assets/Scripts/InGame/` | In-game gameplay domain nav -> `InGame/AGENTS.md` |
| `project-link/Assets/Scripts/InGame/Board/` | Grid data model + cell rendering -> `InGame/Board/AGENTS.md` |
| `project-link/Assets/Scripts/InGame/Path/` | Path drawing, validation, line rendering -> `InGame/Path/AGENTS.md` |
| `project-link/Assets/Scripts/InGame/Input/` | Touch drag, longpress, cell snap, erase -> `InGame/Input/AGENTS.md` |
| `project-link/Assets/Scripts/InGame/UI/` | HUD, timer, popups, circular gauge -> `InGame/UI/AGENTS.md` |
| `project-link/Assets/Scripts/InGame/Camera/` | Large board zoom + pan -> `InGame/Camera/AGENTS.md` |
| `project-link/Assets/Scripts/OutGame/` | Non-gameplay scenes (Title, Lobby) -> `OutGame/AGENTS.md` |
| `project-link/Assets/Scripts/OutGame/UI/` | Title/Lobby UI helpers, outgame popups -> `OutGame/UI/AGENTS.md` |
| `project-link/Assets/Scripts/Editor/` | Unity Editor automation tools -> `Editor/AGENTS.md` |
| `project-link/Assets/Scripts/Generated/` | Unity-synced shared contracts -> `Generated/AGENTS.md` |
| `project-link/Assets/Scripts/Services/` | Client service boundaries for server/static UI data -> `Services/AGENTS.md` |
| `project-link/Assets/Scripts/Data/` | Hand-written data models and loaders -> `Data/AGENTS.md` |
| `project-link/Assets/Scripts/Data/Generated/` | Auto-generated C# models; do not edit -> `Data/Generated/AGENTS.md` |
| `project-link/Assets/Scripts/Utils/` | Stateless helpers: CsvLoader, GridUtils, ColorPalette -> `Utils/AGENTS.md` |
| `project-link/Assets/Prefabs/` | Runtime prefabs |
| `project-link/Assets/Resources/` | Runtime-loadable assets -> `Resources/AGENTS.md` |
| `project-link/Assets/Resources/Data/` | Runtime CSV data; generated from `shared/datas/` |
| `project-link/Assets/Resources/UI/` | Imported UI reference sprites and sliced sheets -> `Resources/UI/AGENTS.md` |
| `project-link/Assets/Resources/Prefabs/UI/` | Runtime-loaded image-backed popup prefabs -> `Resources/Prefabs/UI/AGENTS.md` |

## Rules
- NEVER edit `Assets/Resources/Data/`; source is `shared/datas/`, regenerate with `npm run gen:data`.
- NEW_DIR: create `AGENTS.md` for it + update Nav above.
- Sorting Layers order: Board -> Path -> Node (UI Canvas uses Screen Space Overlay, always on top).
- Physics Layer `Board` reserved for touch input raycasting.

## Cross-refs
| type | refs |
|------|------|
| Depends on | `project-link:docs/refs/platform-auth.md` |
| Platform source | `platform:docs/refs/auth.md` |
| External API | `platform-auth:POST /auth/login`, `platform-auth:POST /auth/refresh`, `platform-auth:POST /auth/logout` |
| Local responsibility | Login/session UX and token storage; do not implement platform account source-of-truth logic. |

## Serena
FIND: `[Domain][Type].cs` -> `find_symbol('[Domain][Type]')`
ENTRY: `Assets/Scripts/InGame/` -> ingame domain root
SKIP: `Assets/Resources/data/` -> auto-generated, navigate source at `shared/datas/`
SKIP: `Assets/Scripts/Data/Generated/` -> auto-generated, navigate source at `shared/datas/`
PATTERN: 1 file = 1 primary class | namespace mirrors folder path

## Conventions
- Namespace: `ProjectLink.InGame.Board`, `ProjectLink.InGame.Path`, etc. mirrors folder structure.
- MonoBehaviour suffix: `View` (e.g. `BoardView`, `PathView`, `NodeView`).
- Pure data/logic classes: no suffix (e.g. `Board`, `PathModel`, `Cell`).
- Input: New Input System via `InputSystem_Actions.inputactions`.
