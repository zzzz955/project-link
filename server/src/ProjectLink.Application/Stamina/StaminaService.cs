using ProjectLink.Contracts.Stamina;
using ProjectLink.Domain.Exceptions;
using ProjectLink.Domain.Interfaces;
using StackExchange.Redis;

namespace ProjectLink.Application.Stamina;

public class StaminaService
{
    private readonly IStaminaRepository          _repo;
    private readonly IStaticDataService          _staticData;
    private readonly IStaminaRefillTransaction   _refillTx;
    private readonly IDatabase                   _redis;

    private static readonly TimeSpan AdTokenTtl = TimeSpan.FromHours(24);

    public StaminaService(
        IStaminaRepository        repo,
        IStaticDataService        staticData,
        IStaminaRefillTransaction refillTx,
        IConnectionMultiplexer    redis)
    {
        _repo       = repo;
        _staticData = staticData;
        _refillTx   = refillTx;
        _redis      = redis.GetDatabase();
    }

    private StaminaResponse ToResponse(Domain.Entities.StaminaState state)
    {
        var config       = _staticData.GetStaminaConfig();
        var nextRecharge = state.Current < config.MaxStamina
            ? state.LastRechargedAt.AddSeconds(config.RechargeSeconds)
            : (DateTimeOffset?)null;

        return new StaminaResponse
        {
            Current        = state.Current,
            Max            = config.MaxStamina,
            NextRechargeAt = nextRecharge?.ToString("O"),
        };
    }

    public async Task<StaminaResponse> GetAsync(string userId, CancellationToken ct)
    {
        var config = _staticData.GetStaminaConfig();
        var state  = await _repo.GetComputedAsync(userId, config.MaxStamina, config.RechargeSeconds / 60, ct);
        return ToResponse(state);
    }

    public async Task<StaminaResponse> DeductAsync(string userId, CancellationToken ct)
    {
        var config = _staticData.GetStaminaConfig();
        var state  = await _repo.DeductAsync(userId, config.MaxStamina, config.RechargeSeconds / 60, ct);
        return ToResponse(state);
    }

    public async Task<StaminaRefillResponse> RefillAsync(string userId, string correlationId, CancellationToken ct)
    {
        var config = _staticData.GetStaminaConfig();
        var result = await _refillTx.ExecuteAsync(
            userId, config.MaxStamina, config.RechargeSeconds, config.RefillCostSoft, correlationId, ct);

        return new StaminaRefillResponse
        {
            Current          = result.CurrentAfter,
            Max              = config.MaxStamina,
            Added            = result.Added,
            SoftCost         = config.RefillCostSoft,
            SoftBalanceAfter = result.SoftBalanceAfter,
            NextRechargeAt   = result.NextRechargeAt?.ToString("O"),
        };
    }

    public async Task<StaminaAdRewardResponse> AdRewardAsync(string userId, string adToken, CancellationToken ct)
    {
        var key = $"ad_token:stamina:{adToken}";
        if (!await _redis.StringSetAsync(key, userId, AdTokenTtl, When.NotExists))
            throw new AdTokenAlreadyUsedException();

        var config      = _staticData.GetStaminaConfig();
        var (state, added) = await _repo.AddAsync(userId, config.MaxStamina, config.RechargeSeconds / 60, config.AdRewardAmount, ct);

        var nextRecharge = state.Current < config.MaxStamina
            ? state.LastRechargedAt.AddSeconds(config.RechargeSeconds)
            : (DateTimeOffset?)null;

        return new StaminaAdRewardResponse
        {
            Current        = state.Current,
            Added          = added,
            NextRechargeAt = nextRecharge?.ToString("O"),
        };
    }

}
