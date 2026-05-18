using ProjectLink.Application.Ranking;
using ProjectLink.Application.StreakChallenge;
using ProjectLink.Contracts.Stage;
using ProjectLink.Domain.Exceptions;
using ProjectLink.Domain.Interfaces;
using ProjectLink.Domain.Stage;
using ProjectLink.Domain.Utilities;

namespace ProjectLink.Application.Stage;

public class StageService
{
    private readonly IStageSessionCache      _sessionCache;
    private readonly IStaminaRepository      _stamina;
    private readonly IInventoryRepository    _inventory;
    private readonly ICurrencyRepository     _currency;
    private readonly IStaticDataService      _staticData;
    private readonly RankingService          _rankingService;
    private readonly IStageEndTransaction    _stageEndTx;
    private readonly StreakChallengeService  _streakChallenge;

    public StageService(
        IStageSessionCache     sessionCache,
        IStaminaRepository     stamina,
        IInventoryRepository   inventory,
        ICurrencyRepository    currency,
        IStaticDataService     staticData,
        RankingService         rankingService,
        IStageEndTransaction   stageEndTx,
        StreakChallengeService streakChallenge)
    {
        _sessionCache    = sessionCache;
        _stamina         = stamina;
        _inventory       = inventory;
        _currency        = currency;
        _staticData      = staticData;
        _rankingService  = rankingService;
        _stageEndTx      = stageEndTx;
        _streakChallenge = streakChallenge;
    }

    public async Task<StageStartResponse> StartAsync(string userId, int stageId, CancellationToken ct)
    {
        var stageData = _staticData.GetStage(stageId)
            ?? throw new StageNotFoundException(stageId);

        var config = _staticData.GetStaminaConfig();
        var stateAfter = await _stamina.DeductAsync(userId, config.MaxStamina, config.RechargeSeconds / 60, ct);

        var now = DateTimeOffset.UtcNow;
        var session = new StageSession
        {
            UserId         = userId,
            StageId        = stageId,
            Token          = IdHelper.NewId(),
            StartAtMs      = now.ToUnixTimeMilliseconds(),
            IsSetupPhase   = true,
            ExtensionCount = 0,
        };

        var buffer = stageData.TimeLimit > 0 ? stageData.TimeLimit + 300 : 3600;
        await _sessionCache.SetAsync(userId, session, TimeSpan.FromSeconds(buffer), ct);

        var allItems = await _inventory.GetAllAsync(userId, ct);
        var ingameItemIds = _staticData.GetAllItems()
            .Select(i => i.ItemId)
            .ToHashSet();
        var itemCounts = allItems
            .Where(i => ingameItemIds.Contains(i.ItemId) && i.Quantity > 0)
            .ToDictionary(i => i.ItemId, i => i.Quantity);

        return new StageStartResponse
        {
            SessionToken     = session.Token,
            ServerStartAt    = now.ToString("O"),
            MoveLimit        = stageData.MoveLimit,
            TimeLimitSeconds = stageData.TimeLimit,
            ItemCounts       = itemCounts,
            StaminaCurrent   = stateAfter.Current,
        };
    }

    public async Task LockAsync(string userId, int stageId, string sessionToken, CancellationToken ct)
    {
        var session = await _sessionCache.GetAsync(userId, ct)
            ?? throw new StageSessionNotFoundException();

        if (session.StageId != stageId || session.Token != sessionToken)
            throw new StageSessionNotFoundException();

        if (!session.IsSetupPhase)
            throw new StageAlreadyLockedException();

        session.IsSetupPhase = false;
        session.StartAtMs    = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var stageData = _staticData.GetStage(stageId)!;
        var ttl = stageData.TimeLimit > 0 ? stageData.TimeLimit + 60 : 3600;
        await _sessionCache.SetAsync(userId, session, TimeSpan.FromSeconds(ttl), ct);
    }

    public async Task<StageEndResponse> EndAsync(
        string userId, int stageId, string sessionToken,
        string result, long clientElapsedMs, int movesUsed,
        string correlationId, CancellationToken ct)
    {
        var session = await _sessionCache.GetAsync(userId, ct)
            ?? throw new StageSessionNotFoundException();

        if (session.StageId != stageId || session.Token != sessionToken)
            throw new StageSessionNotFoundException();

        if (result != "success" && result != "fail")
            throw new InvalidStageResultException();

        await _sessionCache.DeleteAsync(userId, ct);

        var stageData = _staticData.GetStage(stageId)!;

        if (result == "fail")
        {
            var failStreakResult = await _streakChallenge.ProcessStageResultAsync(
                userId, stageId, isFirstClear: false, isMainStage: true, result: "fail", ct);

            return new StageEndResponse
            {
                Score             = 0,
                Stars             = 0,
                MovesUsed         = movesUsed,
                MoveLimit         = stageData.MoveLimit,
                NextStageId       = null,
                NextStageUnlocked = false,
                StreakChallenge   = failStreakResult,
            };
        }

        // Validate move limit
        if (stageData.MoveLimit > 0 && movesUsed > stageData.MoveLimit)
            throw new InvalidStageResultException();

        var serverElapsedMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - session.StartAtMs;
        var config          = _staticData.GetStaminaConfig();
        var adjustedMs      = RankingService.AdjustElapsedMs(clientElapsedMs, serverElapsedMs, _rankingService.NetworkToleranceMs);
        var score           = RankingService.ComputeScore(stageData.TimeLimit, adjustedMs);
        var maxScore        = stageData.TimeLimit * 100;
        var stars           = score >= maxScore * 3 / 4 ? 3 : score >= maxScore / 2 ? 2 : 1;

        var allStages  = _staticData.GetAllStages();
        var maxStageId = allStages.Count > 0 ? allStages.Max(s => s.StageId) : stageId;

        var dbResult = await _stageEndTx.ExecuteAsync(new StageEndDbCommand
        {
            UserId                  = userId,
            StageId                 = stageId,
            Stars                   = stars,
            Score                   = score,
            AdjustedMs              = adjustedMs,
            SoftReward              = stageData.SoftReward,
            StaminaRefund           = 1,
            MaxStamina              = config.MaxStamina,
            RechargeIntervalMinutes = config.RechargeSeconds / 60,
            MovesUsed               = movesUsed,
            MaxStages               = maxStageId,
            CorrelationId           = correlationId,
        }, ct);

        // Update Redis ranking best-effort (non-transactional, recoverable from DB)
        if (dbResult.IsBestRecord)
            await _rankingService.OnStageEndAsync(userId, stageId, adjustedMs, score, dbResult.TotalScore, dbResult.StagesCleared, ct);

        var rankPercentile = await _rankingService.GetStageRankPercentileAsync(userId, stageId, ct);

        var streakResult = await _streakChallenge.ProcessStageResultAsync(
            userId, stageId, isFirstClear: dbResult.IsFirstClear, isMainStage: true, result: "success", ct);

        var nextStageId = stageId < maxStageId ? stageId + 1 : (int?)null;

        return new StageEndResponse
        {
            Score             = score,
            Stars             = stars,
            AdjustedElapsedMs = adjustedMs,
            IsBestRecord      = dbResult.IsBestRecord,
            SoftBalanceAfter  = dbResult.SoftBalanceAfter,
            SoftReward        = dbResult.SoftRewardGranted,
            MovesUsed         = movesUsed,
            MoveLimit         = stageData.MoveLimit,
            NextStageId       = streakResult?.NavigationDirective is "RETURN_TO_LOBBY" or "OPEN_REWARD_POPUP" ? null : nextStageId,
            NextStageUnlocked = dbResult.NextStageUnlocked,
            RankPercentile    = rankPercentile,
            StreakChallenge   = streakResult,
        };
    }

    public async Task<StageExtendResponse> ExtendAsync(string userId, int stageId, string sessionToken, string correlationId, CancellationToken ct)
    {
        var session = await _sessionCache.GetAsync(userId, ct)
            ?? throw new StageSessionNotFoundException();

        if (session.StageId != stageId || session.Token != sessionToken)
            throw new StageSessionNotFoundException();

        var nextCount  = session.ExtensionCount + 1;
        var extConfig  = _staticData.GetTimeExtendConfig(nextCount)
            ?? throw new InvalidStageResultException();

        var balanceAfter = await _currency.DeductAsync(
            userId, extConfig.CostSoft, "time_extend", IdHelper.NewId(), correlationId, ct);

        session.ExtensionCount = nextCount;

        var newTtl = TimeSpan.FromSeconds(extConfig.ExtendSeconds + 60);
        await _sessionCache.SetAsync(userId, session, newTtl, ct);

        return new StageExtendResponse
        {
            ExtendedSeconds  = extConfig.ExtendSeconds,
            SoftBalanceAfter = balanceAfter,
        };
    }

    public async Task RecordItemUseAsync(string userId, int stageId, string sessionToken, List<ItemUsedEntry> items, CancellationToken ct)
    {
        var session = await _sessionCache.GetAsync(userId, ct)
            ?? throw new StageSessionNotFoundException();

        if (session.StageId != stageId || session.Token != sessionToken)
            throw new StageSessionNotFoundException();

        if (!session.IsSetupPhase)
            throw new StageNotInSetupPhaseException();

        session.ItemsUsed.AddRange(items);

        var stageData = _staticData.GetStage(stageId)!;
        var buffer = stageData.TimeLimit > 0 ? stageData.TimeLimit + 300 : 3600;
        await _sessionCache.SetAsync(userId, session, TimeSpan.FromSeconds(buffer), ct);
    }
}
