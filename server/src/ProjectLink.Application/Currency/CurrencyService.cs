using ProjectLink.Contracts.Currency;
using ProjectLink.Domain.Exceptions;
using ProjectLink.Domain.Interfaces;
using ProjectLink.Domain.Utilities;
using StackExchange.Redis;

namespace ProjectLink.Application.Currency;

public class CurrencyService
{
    private readonly ICurrencyRepository _repo;
    private readonly IDatabase           _redis;
    private readonly IConfiguration      _config;

    private static readonly TimeSpan AdTokenTtl = TimeSpan.FromHours(24);

    public CurrencyService(ICurrencyRepository repo, IConnectionMultiplexer redis, IConfiguration config)
    {
        _repo   = repo;
        _redis  = redis.GetDatabase();
        _config = config;
    }

    public async Task<CurrencyResponse> GetAsync(string userId, CancellationToken ct)
    {
        var balance = await _repo.GetBalanceAsync(userId, ct);
        return new CurrencyResponse { SoftAmount = balance };
    }

    public Task<long> DeductAsync(string userId, long amount, string reason, string correlationId, CancellationToken ct)
        => _repo.DeductAsync(userId, amount, reason, IdHelper.NewId(), correlationId, ct);

    public Task<long> GrantAsync(string userId, long amount, string reason, string correlationId, CancellationToken ct)
        => _repo.GrantAsync(userId, amount, reason, IdHelper.NewId(), correlationId, ct);

    public async Task<CurrencyAdRewardResponse> AdRewardAsync(string userId, string adToken, string correlationId, CancellationToken ct)
    {
        var key = $"ad_token:currency:{adToken}";
        if (!await _redis.StringSetAsync(key, userId, AdTokenTtl, When.NotExists))
            throw new AdTokenAlreadyUsedException();

        var rewardAmount = _config.GetValue<long>("Currency:AdRewardSoft", 50);
        var balanceAfter = await _repo.GrantAsync(userId, rewardAmount, "ad_reward", adToken, correlationId, ct);

        return new CurrencyAdRewardResponse
        {
            SoftAmountAfter = balanceAfter,
            Added           = (int)rewardAmount
        };
    }
}
