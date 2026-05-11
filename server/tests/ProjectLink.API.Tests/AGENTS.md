# ProjectLink.API.Tests

## Files
| file | class | role |
|------|-------|------|
| `ProjectLink.API.Tests.csproj` | project | xUnit API integration test project |
| `AuthIntegrationTests.cs` | `AuthIntegrationTests` | Verifies JWT auth, invalid token rejection, and guest-login to lobby round trip |
| `ApiTestFactory.cs` | `ApiTestFactory` | WebApplicationFactory with in-memory repositories and fake platform auth |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `AuthIntegrationTests.ValidPlatformJwt_CanFetchLobby` | test | Authenticated endpoint accepts app-scoped JWT |
| `AuthIntegrationTests.InvalidOrExpiredJwt_ReturnsUnauthorized` | test | Invalid and expired JWTs are rejected |
| `AuthIntegrationTests.PlatformGuestLoginToken_CanFetchLobby` | test | Guest login proxy token can call lobby without Unity |
| `ApiTestFactory.CreatePlatformToken` | method | Creates signed JWTs matching test auth options |
| `ApiTestFactory.ConfigureWebHost` | method | Replaces external DB/Redis/platform dependencies with test fakes |

## Cross-refs
- Depends on: `ProjectLink.API.Program`, `ProjectLink.API.Controllers.AuthController`, `ProjectLink.API.Controllers.LobbyController`
- Depends on: `ProjectLink.Application.Lobby.LobbyService`
- Consumed by: `server/src/ProjectLink.sln`

## Rules
- Keep tests deterministic and engine-free.
- Do not connect to real Postgres, Redis, or platform auth from this project.
