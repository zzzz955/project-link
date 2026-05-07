# ProjectLink.Application

## Nav
| path | role |
|------|------|
| `Bootstrap/` | App version + server time response | → `Bootstrap/AGENTS.md` |
| `Stage/` | Stage lifecycle (start/lock/end/extend) | → `Stage/AGENTS.md` |
| `Stamina/` | Stamina state, ad-reward, refill | → `Stamina/AGENTS.md` |
| `Ranking/` | Redis leaderboard build and update | → `Ranking/AGENTS.md` |
| `Lobby/` | Full lobby state snapshot | → `Lobby/AGENTS.md` |
| `Progress/` | Batch stage-progress query with unlock computation | → `Progress/AGENTS.md` |
| `DailyChallenge/` | Daily challenge state and completion | → `DailyChallenge/AGENTS.md` |
| `Shop/` | Shop catalog and soft-currency item purchase | → `Shop/AGENTS.md` |
| `Settings/` | Server-synced player preferences | → `Settings/AGENTS.md` |
| `Currency/` | Soft currency ad-reward |
| `Inventory/` | Item inventory reads and use |
| `Session/` | `ISessionCache` interface + `SessionService` |
| `UserProfile/` | Display name, avatar reads and updates |

## Rules
- Use-case layer: no direct DB access — all persistence via repository interfaces from `Domain.Interfaces`
- Services return contract DTOs (`ProjectLink.Contracts.*`) — never expose domain entities
- `async/await` throughout; CancellationToken passed to all async methods
