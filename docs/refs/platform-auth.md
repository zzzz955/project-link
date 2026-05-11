# Platform Auth Refs

## Source of Truth
| item | ref |
|------|-----|
| Platform feature index | `platform:docs/refs/auth.md` |
| Platform architecture | `platform:docs/architecture/auth-security.md` |
| Platform contracts | `platform:packages/contracts/auth` |
| Platform auth service | `platform:services/auth` |

## Project Link Consumers
| area | responsibility |
|------|----------------|
| `project-link:server` | Validate platform access JWTs offline using issuer, audience, expiry, signature, `kid`, and required claims. |
| `project-link:client` | Provide login/session UX and store issued tokens according to platform guidance. |
| `project-link:shared/contracts` | Keep game-specific contracts separate from platform account source-of-truth contracts. |

## Public Surface Used
| surface | ref |
|---------|-----|
| Login | `platform-auth:POST /auth/login` |
| Google native ID token login | `platform-auth:POST /auth/google` |
| Refresh | `platform-auth:POST /auth/refresh` |
| Logout | `platform-auth:POST /auth/logout` |
| Current account | `platform-auth:GET /users/me` |
| JWKS | `platform-auth:GET /.well-known/jwks.json` |
| OIDC discovery | `platform-auth:GET /.well-known/openid-configuration` |

## Local Rules
- Project Link must not implement account identity, social identity linking, refresh-token family state, key rotation, lockout, recovery, or audit source-of-truth logic.
- Project Link server validates access tokens offline and treats refresh/re-login as platform auth responsibilities.
- Project Link client may initiate guest and Google native account selection flows, then send provider ID tokens/results to platform auth over HTTPS.
- Project Link mobile login must not require web redirect for Google in the MVP path.
- If platform auth behavior is unclear, update `platform:docs/refs/auth.md` or the platform contract first.

## Agent Lookup Order
1. Read this file.
2. Read `platform:docs/refs/auth.md` (`../platform/docs/refs/auth.md`).
3. Read `platform:docs/architecture/auth-security.md` for security behavior.
4. Read local server/client `AGENTS.md` for consumer responsibilities.
5. Read platform contracts or architecture docs only when needed.
6. Inspect implementation files only after refs and contracts are insufficient.

## See Also
- `project-link:docs/refs/platform-infra.md` — infra topology and env var conventions (AUTH_USE_MOCK, JWT_AUTHORITY)
