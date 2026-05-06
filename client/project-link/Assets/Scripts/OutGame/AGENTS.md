# OutGame — Non-gameplay scenes (Title, Lobby)

## Nav
| path | role |
|---|---|
| `UI/` | Title/Lobby UI helpers, navigation, outgame popups → `UI/AGENTS.md` |

## Rules
- Namespace: `ProjectLink.OutGame.[SubDir]` mirrors folder structure
- Scene navigation must always go through `SceneLoader.LoadScene` (with fade)
- Cross-scene state must go through `GameContext`
