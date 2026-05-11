# server/tests

## Nav
| path | role |
|------|------|
| `ProjectLink.API.Tests/` | ASP.NET Core API auth and integration tests -> `ProjectLink.API.Tests/AGENTS.md` |

## Rules
- Tests run outside Unity and must not require the Unity editor.
- Prefer in-memory fakes for API boundary tests unless the test explicitly validates Docker infrastructure.
- NEW_DIR: create `AGENTS.md` for it + update Nav above.
