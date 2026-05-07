using ProjectLink.Contracts.Stamina;
using ProjectLink.Domain.Exceptions;
using ProjectLink.Domain.Interfaces;
using StackExchange.Redis;

namespace ProjectLink.Application.Stamina;

public class StaminaService
{
    private readonly IStaminaRepository  _repo;
    private readonly ICurrencyRepository _currency;
    private readonly IDatabase           _redis;
    private readonly IConfiguration      _config;

    private static readonly TimeSpan AdTokenTtl = TimeSpan.FromHours(24);

    public StaminaService(IStaminaRepository repo, ICurrencyRepository currency, IConnectionMultiplexer redis, IConfiguration config)
    {
        _repo     = repo;
        _currency = currency;
        _redis    = redis.GetDatabase();
        _config   = config;
    }

    private int  MaxStamina          => _config.GetValue<int>("Stamina:Max", 5);
    private int  RechargeIntervalMin => _config.GetValue<int>("Stamina:RechargeIntervalMinutes", 30);
    private int  AdRewardAmount      => _config.GetValue<int>("Stamina:AdRewardAmount", 1);
    private long ExtendCostSoft      => _config.GetValue<long>("Stamina:ExtendCostSoft", 30);

    private StaminaResponse ToResponse(Domain.Entities.StaminaState state)
    {
        var nextRecharge = state.Current < MaxStamina
            ? state.LastRechargedAt.AddMinutes(RechargeIntervalMin)
            : (DateTimeOffset?)null;

        return new StaminaResponse
        {
            Current        = state.Current,
            Max            = MaxStamina,
            NextRechargeAt = nextRecharge?.ToString("O")
        };
    }

    public async Task<StaminaResponse> GetAsync(string userId, CancellationToken ct)
    {
        var state = await _repo.GetComputedAsync(userId, MaxStamina, RechargeIntervalMin, ct);
        return ToResponse(state);
    }

    public async Task<StaminaResponse> DeductAsync(string userId, CancellationToken ct)
    {
        var state = await _repo.DeductAsync(userId, MaxStamina, RechargeIntervalMin, ct);
        return ToResponse(state);
    }

    public async Task<StaminaAdRewardResponse> AdRewardAsync(string userId, string adToken, CancellationToken ct)
    {
        var key = $"ad_token:stamina:{adToken}";
        if (!await _redis.StringSetAsync(key, userId, AdTokenTtl, When.NotExists))
            throw new AdTokenAlreadyUsedException();

        var (state, added) = await _repo.AddAsync(userId, MaxStamina, RechargeIntervalMin, AdRewardAmount, ct);

        var nextRecharge = state.Current < MaxStamina
            ? state.LastRechargedAt.AddMinutes(RechargeIntervalMin)
            : (DateTimeOffset?)null;

        return new StaminaAdRewardResponse
        {
            Current        = state.Current,
            Added          = added,
            NextRechargeAt = nextRecharge?.ToString("O")
        };
    }

    public async Task<StaminaExtendResponse> ExtendAsync(string userId, string correlationId, CancellationToken ct)
    {
        var balanceAfter = await _currency.DeductAsync(
            userId, ExtendCostSoft, "stamina_extend", Guid.NewGuid().ToString(), correlationId, ct);

        var (state, _) = await _repo.AddAsync(userId, MaxStamina, RechargeIntervalMin, 1, ct);

        return new StaminaExtendResponse
        {
            StaminaCurrent   = state.Current,
            SoftBalanceAfter = balanceAfter
        };
    }
}
