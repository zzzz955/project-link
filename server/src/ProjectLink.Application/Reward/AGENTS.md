# ProjectLink.Application/Reward

## Files
| file | class | role |
|---|---|---|
| `RewardService.cs` | `RewardService` | Generic reward claim use case for UI reward/ad popup binding |

## Symbols
| symbol | kind | note |
|---|---|---|
| `RewardService.ClaimAsync(...)` | method | idempotent reward-token claim; grants configured soft currency and returns `RewardClaimResponse` |
| `RewardService.ResolveSoftAmount(string)` | method | maps reward source to configured soft currency amount |

## Cross-refs
- Consumed by: server `API.Controllers.RewardController`
- Depends on: `Domain.Interfaces.ICurrencyRepository`, Redis `IConnectionMultiplexer`

## Rules
- Detailed ad/IAP verification remains outside this service until platform validation is implemented.
- Reward token must be idempotent through Redis before currency grant.
