using ProjectLink.Contracts.Reward;
using ProjectLink.Domain.Exceptions;
using ProjectLink.Domain.Interfaces;
using StackExchange.Redis;

namespace ProjectLink.Application.Reward;

public class RewardService
{
    private readonly ICurrencyRepository _currency;
    private readonly IDatabase           _redis;
    private readonly IConfiguration      _config;

    static readonly TimeSpan RewardTokenTtl = TimeSpan.FromHours(24);

    public RewardService(ICurrencyRepository currency, IConnectionMultiplexer redis, IConfiguration config)
    {
        _currency = currency;
        _redis    = redis.GetDatabase();
        _config   = config;
    }

    public async Task<RewardClaimResponse> ClaimAsync(
        string userId,
        string rewardSource,
        string rewardToken,
        int multiplier,
        string correlationId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(rewardSource))
            rewardSource = "default";

        if (string.IsNullOrWhiteSpace(rewardToken))
            rewardToken = Guid.NewGuid().ToString();

        multiplier = Math.Clamp(multiplier, 1, 3);

        var key = $"reward_token:{rewardSource}:{rewardToken}";
        if (!await _redis.StringSetAsync(key, userId, RewardTokenTtl, When.NotExists))
            throw new AdTokenAlreadyUsedException();

        var softAmount = ResolveSoftAmount(rewardSource) * multiplier;
        var response = new RewardClaimResponse();

        if (softAmount > 0)
        {
            var balanceAfter = await _currency.GrantAsync(
                userId,
                softAmount,
                $"reward:{rewardSource}",
                rewardToken,
                correlationId,
                ct);

            response.SoftBalanceAfter = balanceAfter;
            response.RewardsGranted.Add(new RewardGrantEntry
            {
                RewardType = "SOFT_CURRENCY",
                RewardId = 0,
                Amount = (int)softAmount,
            });
        }
        else
        {
            response.SoftBalanceAfter = await _currency.GetBalanceAsync(userId, ct);
        }

        return response;
    }

    long ResolveSoftAmount(string rewardSource)
    {
        var configured = _config.GetValue<long?>($"Reward:{rewardSource}:Soft");
        if (configured.HasValue) return configured.Value;

        return rewardSource switch
        {
            "ad_video" => _config.GetValue<long>("Currency:AdRewardSoft", 50),
            "daily_login" => _config.GetValue<long>("Reward:DailyLoginSoft", 100),
            _ => _config.GetValue<long>("Reward:DefaultSoft", 0),
        };
    }
}
