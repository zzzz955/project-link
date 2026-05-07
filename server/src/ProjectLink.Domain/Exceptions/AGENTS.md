# ProjectLink.Domain/Exceptions

## Files
| file | class | maps to |
|------|-------|---------|
| `DomainException.cs` | `DomainException` | abstract base — `ErrorCode` string + message |
| `InsufficientFundsException.cs` | `InsufficientFundsException` | HTTP 422 — INSUFFICIENT_FUNDS |
| `InsufficientStaminaException.cs` | `InsufficientStaminaException` | HTTP 422 — INSUFFICIENT_STAMINA |
| `InsufficientInventoryException.cs` | `InsufficientInventoryException` | HTTP 422 — INSUFFICIENT_INVENTORY |
| `StageSessionNotFoundException.cs` | `StageSessionNotFoundException` | HTTP 404 — STAGE_SESSION_NOT_FOUND |
| `StageAlreadyActiveException.cs` | `StageAlreadyActiveException` | HTTP 409 — STAGE_ALREADY_ACTIVE |
| `StageAlreadyLockedException.cs` | `StageAlreadyLockedException` | HTTP 409 — STAGE_ALREADY_LOCKED |
| `StageNotInSetupPhaseException.cs` | `StageNotInSetupPhaseException` | HTTP 409 — STAGE_NOT_IN_SETUP_PHASE |
| `StageNotFoundException.cs` | `StageNotFoundException` | HTTP 404 — STAGE_NOT_FOUND |
| `AdTokenAlreadyUsedException.cs` | `AdTokenAlreadyUsedException` | HTTP 409 — AD_TOKEN_ALREADY_USED |
| `InvalidStageResultException.cs` | `InvalidStageResultException` | HTTP 400 — INVALID_STAGE_RESULT |
| `DailyChallengeAlreadyCompletedException.cs` | `DailyChallengeAlreadyCompletedException` | HTTP 409 — DAILY_CHALLENGE_ALREADY_COMPLETED |
| `DailyChallengeNotCompletableException.cs` | `DailyChallengeNotCompletableException` | HTTP 422 — DAILY_CHALLENGE_NOT_COMPLETABLE |
| `StaminaAlreadyFullException.cs` | `StaminaAlreadyFullException` | HTTP 422 — STAMINA_ALREADY_FULL |
| `ShopItemNotFoundException.cs` | `ShopItemNotFoundException` | HTTP 404 — SHOP_ITEM_NOT_FOUND |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `DomainException.ErrorCode` | property | error code string returned to client; maps to `error_messages.csv` |

## Cross-refs
- Consumed by: server `Application.*Service` (thrown on validation failure)
- Consumed by: server `Infrastructure.Persistence.*TransactionRepository` (thrown on lock/validation failure)
- Consumed by: server `API.Middleware.GlobalExceptionMiddleware` (caught + mapped to HTTP status codes)

## Rules
- Every `ErrorCode` value must have a corresponding row in `shared/datas/string/error_messages.csv`
- HTTP status mapping is owned by `GlobalExceptionMiddleware` — do not embed status codes in exceptions
