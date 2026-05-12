# 18 - Infrastructure

Scope: Docker Desktop dev setup, production container exposure, shared auth platform ports, game server stack ports, SSH-only operations access.

---

## Port Allocation

Use 100-port blocks per platform or game. Container ports stay standard; only host ports change.

| Block | Owner | Public API | DB | Redis | Admin/Tool | Metrics |
|-------|-------|------------|----|-------|------------|---------|
| 20000-20099 | Shared Auth Platform | 20001 | 20032 | 20079 | 20080 | 20090 |
| 20100-20199 | Game 1 | 20101 | 20132 | 20179 | 20180 | 20190 |
| 20200-20299 | Game 2 | 20201 | 20232 | 20279 | 20280 | 20290 |
| 20300-20399 | Game 3 | 20301 | 20332 | 20379 | 20380 | 20390 |

- [x] Reserve `20000-20099` for the shared auth platform
- [x] Assign each game a dedicated 100-port block before creating its Compose project
- [x] Keep API, DB, Redis, admin/tool, and metrics offsets consistent in every block
- [x] Document each assigned block in this file before opening ports

---

## Docker Compose Profiles

- [x] Keep one Compose network per stack so `api` reaches `db:5432` and `redis:6379`
- [x] Keep container ports standard: API `8080`, PostgreSQL `5432`, Redis `6379`
- [x] Use distinct host ports only when the host machine must connect directly
- [ ] Add `.env.dev.example` for local direct access values
- [ ] Add `.env.prod.example` for production-safe localhost-bound values
- [ ] Decide whether to split Compose into `docker-compose.yml`, `docker-compose.dev.yml`, and `docker-compose.prod.yml`

---

## Docker Desktop (Dev Monitoring)

Docker Desktop is the primary tool for checking container state in dev.

| task | where |
|------|-------|
| Check running containers and health | Docker Desktop → Containers |
| Read container logs (stdout/stderr) | Docker Desktop → Container → Logs tab |
| Inspect env vars and port bindings | Docker Desktop → Container → Inspect tab |
| Check resource usage (CPU/mem) | Docker Desktop → Container → Stats tab |

Convention:
- Before reporting a dev issue, check Docker Desktop → Containers to confirm all services are running.
- If a container has exited unexpectedly, check the Logs tab before restarting.
- `docker compose up -d` from WSL CLI is the authoritative startup; Docker Desktop is read-only monitoring.

---

## Dev Exposure

Dev should allow direct access from the developer PC.

- [x] Expose API on the game block API port, e.g. `20101:8080`
- [x] Expose PostgreSQL on localhost for direct host-side access, e.g. `127.0.0.1:20132:5432`
- [x] Remove Redis `ports:` unless direct Redis debugging becomes necessary
- [ ] Verify Unity/client calls API through the configured dev API host port
- [ ] Verify API connects internally with `Host=db;Port=5432`
- [ ] Verify API connects internally with `redis:6379`

---

## Production Exposure

Production clients must only use `80/443`. DB and Redis must not be internet-reachable.

- [ ] Put Nginx, Caddy, ALB, or equivalent reverse proxy in front of API
- [ ] Route `https://auth.example.com` to the auth platform API port
- [ ] Route each game domain to that game's API port
- [ ] Open public firewall/security-group ingress only for `80`, `443`, and restricted `22`
- [ ] Bind production DB host ports to localhost only, e.g. `127.0.0.1:20132:5432`
- [ ] Remove Redis `ports:` in production unless SSH-tunnel debugging is required
- [ ] If Redis tunnel access is required, bind only to localhost, e.g. `127.0.0.1:20179:6379`
- [ ] Confirm DB/Redis ports are not reachable from an external network scan

---

## Operations Access

- [ ] Require SSH key authentication for production servers
- [ ] Disable SSH password login
- [ ] Restrict SSH source IPs where infrastructure allows it
- [ ] Access production PostgreSQL through SSH tunnel only
- [ ] Create a readonly DB role for ops inspection
- [ ] Keep app DB role separate from readonly ops and migration/admin roles
- [ ] Use migration/admin credentials only for schema changes
- [ ] Define emergency data-fix procedure with reviewed SQL scripts

Example tunnel:

```bash
ssh -L 20132:127.0.0.1:20132 user@prod-server
```

---

## Secrets and Build Context

- [x] Keep `.env` out of git
- [x] Keep `.env` in root `.dockerignore`
- [x] Exclude `.git`, `**/node_modules`, `**/bin`, and `**/obj` from Docker build context
- [x] Use `.env.example` files for variable names only, never real secrets
- [ ] Use separate secret values for dev, staging, and production
- [ ] Disable mock authentication in production
- [ ] Rotate production DB, Redis, JWT, and auth platform secrets before launch

---

## Production Hardening

- [ ] Enforce HTTPS at the reverse proxy
- [ ] Configure request size limits
- [ ] Configure rate limits for auth and API endpoints
- [ ] Enable structured access logs on the reverse proxy
- [ ] Enable API structured logs with correlation IDs
- [ ] Add container restart policies
- [ ] Add health checks for API, DB, and Redis
- [ ] Add volume backup policy for PostgreSQL
- [ ] Add restore drill checklist for PostgreSQL backups
- [ ] Add monitoring for container health, disk usage, memory, CPU, and DB connections
- [ ] Define alert channels for production incidents

---

## Verification

- [x] `docker compose config` passes for dev configuration
- [x] `docker compose build api` passes
- [ ] `docker compose up -d` starts API, DB, and Redis
- [ ] API health endpoint returns healthy DB/Redis status
- [ ] External client can reach production API only through `https://`
- [ ] External client cannot reach production DB/Redis ports directly

<!-- changed: added infrastructure tracker for Docker dev/prod exposure, port blocks, SSH-only ops access, and production hardening -->
<!-- changed: applied dev Compose port policy and Docker build context hardening -->
