# Platform Infra Refs

## Source of Truth
| item | ref |
|------|-----|
| Base compose | `platform:docker-compose.yml` |
| Dev overlay | `platform:docker-compose.dev.yml` |
| Prod overlay | `platform:docker-compose.prod.yml` |
| Dev env template | `platform:.env.dev.example` |
| Prod env template | `platform:.env.prod.example` |

## Infra Design
| concern | decision |
|---------|----------|
| Environment split | `docker-compose.yml` (base) + overlay (`docker-compose.dev.yml` or `docker-compose.prod.yml`) |
| Dev env file | `.env.dev` (gitignored); copied from `.env.dev.example` |
| Prod env file | `.env.prod` (gitignored); copied from `.env.prod.example` |
| Dev startup | `docker compose --env-file .env.dev -f docker-compose.yml -f docker-compose.dev.yml up -d` |
| Prod startup | `docker compose --env-file .env.prod -f docker-compose.yml -f docker-compose.prod.yml up -d` |
| Cross-service network | `madalang-net` bridge (platform creates it; game server joins via prod overlay) |

## Port Policy
| range | owner |
|-------|-------|
| 20000–20099 | Platform (auth: 20001, db: 20032) |
| 20100–20199 | Project Link (api: 20101, db: 20132) |

## Game Server ↔ Platform Auth Connection
| env | JWT_AUTHORITY | AUTH_USE_MOCK | note |
|-----|---------------|---------------|------|
| dev | `http://localhost:20001` | `true` | mock auth; JWKS fetch silently fails — benign |
| prod | `http://platform-auth:8080` | `false` | reachable via `madalang-net`; platform stack must be running |

## Local Rules
- DB ports must be bound to `127.0.0.1` in prod (SSH tunnel for admin access).
- `AUTH_USE_MOCK=false` requires platform stack running and game server on `madalang-net`.
- Game server joins `madalang-net` only in prod overlay (`docker-compose.prod.yml`).
- Client prod URLs are placeholders until production deployment is ready — see `project-link:client/Scripts/Core/AppEnvironment.cs`.
