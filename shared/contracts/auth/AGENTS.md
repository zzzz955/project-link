# packages/contracts/auth/src/Auth.Contracts

## Files
| file | class/module | role |
|------|--------------|------|
| `Auth.Contracts.csproj` | Project | netstandard2.1 class library; no service/DB/ASP.NET Core dependencies. |
| `Requests.cs` | Auth request DTOs | Guest login, Google login, refresh, logout, revoke, recovery, session validation, admin revoke. |
| `Responses.cs` | Auth response DTOs | Token pair, session, account profile, revoke result, session state. |
| `Errors.cs` | Auth error types | Error envelope with machine-readable codes, trace id, retryability, required action, field details. |
| `Claims.cs` | JWT claim constants | Claim name constants and canonical value constants for account type, state, provider, token use. |
| `Events.cs` | Auth event DTOs | Base event with idempotency fields; derived events for session revoke, family compromise, account lock/recovery/ban, key rotation, security state change. |
| `AGENTS.md` | Docs | Directory instructions. |
| `CLAUDE.md` | Docs | Claude wrapper. |

## Symbols
| symbol | kind | note |
|--------|------|------|
| GuestLoginRequest | record | POST /auth/guest request. |
| GoogleLoginRequest | record | POST /auth/google request. |
| RefreshRequest | record | POST /auth/refresh request. |
| LogoutRequest | record | POST /auth/logout request; Reason is optional. |
| RevokeRequest | record | POST /auth/revoke request; Reason is optional. |
| RecoveryInitRequest | record | Recovery init request (P1). |
| AdminRevokeRequest | record | Admin session revoke request (P1). |
| SessionValidationRequest | record | Introspection request (P1). |
| AuthTokenResponse | record | Access/refresh token pair with expiry and token type. |
| AuthSessionResponse | record | Session result including account id, session id, account type, client id, and token pair. |
| AccountProfileResponse | record | Account profile for /users/me. |
| RevokeResponse | record | Revoke operation result. |
| SessionStateResponse | record | Session introspection result (P1). |
| AuthError | record | Error envelope: code, message, trace id, retryability, required action, optional field errors. |
| AuthFieldError | record | Per-field validation error inside AuthError. |
| AuthErrorCodes | static class | Machine-readable error code constants. |
| AuthClaims | static class | JWT claim name constants aligned with OIDC where applicable. |
| AuthClaimValues | static class | Canonical values for AccountType, AccountState, TokenUse, Provider claims. |
| AuthEvent | abstract record | Base event with EventId, EventType, Version, OccurredAt, AccountId, SessionId, Reason. |
| AuthEventTypes | static class | Event type name string constants. |
| SessionRevokedEvent | record | Session revoked; adds FamilyId. |
| TokenFamilyCompromisedEvent | record | Token family reuse detected; adds FamilyId. |
| AccountLockedEvent | record | Account locked; adds LockExpiresAt, FailureCount. |
| AccountRecoveredEvent | record | Account recovered; adds RecoveryMethod. |
| AccountBannedEvent | record | Account banned; adds BanExpiresAt. |
| AccountUnbannedEvent | record | Account unbanned. |
| SigningKeyRotatedEvent | record | Signing key rotated; adds OldKid, NewKid, OldKeyRetireAt. |
| UserSecurityStateChangedEvent | record | Security state changed; adds PreviousState, NewState. |

## Cross-refs
| type | refs |
|------|------|
| Consumed by | `Platform.Auth.Api`, `Platform.Auth.Infrastructure`, game servers, future `apps/web` |
| Depends on | nothing (no service/DB/provider dependencies) |
| Feature index | `platform:docs/refs/auth.md` |
| Distribution | `platform:tools/auth/auth-packet-generator` |

## Rules
- Namespace root is `Madalang.Platform.Auth.Contracts.V1`; introduce V2 only for breaking changes.
- No ASP.NET Core, EF Core, database, Redis, or Google SDK dependencies.
- Keep token claim names aligned with OIDC where practical.
- Do not expose provider access tokens or raw private key material in any contract type.
- Every event derived type must set EventType to the matching AuthEventTypes constant on construction.
