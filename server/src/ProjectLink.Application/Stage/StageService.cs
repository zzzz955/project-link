using ProjectLink.Application.Ranking;
using ProjectLink.Contracts.Stage;
using ProjectLink.Domain.Exceptions;
using ProjectLink.Domain.Interfaces;
using ProjectLink.Domain.Stage;

namespace ProjectLink.Application.Stage;

public class StageService
{
    private readonly IStageSessionCache  _sessionCache;
    private readonly IStaminaRepository  _stamina;
    private readonly ICurrencyRepository _currency;
    private readonly IRankingRepository  _ranking;
    private readonly IProgressRepository _progress;
    private readonly IStaticDataService  _staticData;
    private readonly RankingService      _rankingService;
    private readonly IConfiguration      _config;

    public StageService(
        IStageSessionCache  sessionCache,
        IStaminaRepository  stamina,
        ICurrencyRepository currency,
        IRankingRepository  ranking,
        IProgressRepository progress,
        IStaticDataService  staticData,
        RankingService      rankingService,
        IConfiguration      config)
    {
        _sessionCache   = sessionCache;
        _stamina        = stamina;
        _currency       = currency;
        _ranking        = ranking;
        _progress       = progress;
        _staticData     = staticData;
        _rankingService = rankingService;
        _config         = config;
    }

    private int MaxStamina          => _config.GetValue<int>("Stamina:Max", 5);
    private int RechargeIntervalMin => _config.GetValue<int>("Stamina:RechargeIntervalMinutes", 30);

    public async Task<StageStartResponse> StartAsync(string userId, int stageId, CancellationToken ct)
    {
        var stageData = _staticData.GetStage(stageId)
            ?? throw new StageNotFoundException(stageId);

        var existing = await _sessionCache.GetAsync(userId, ct);
        if (existing != null)
            throw new StageAlreadyActiveException();

        // Deduct 1 stamina
        await _stamina.DeductAsync(userId, MaxStamina, RechargeIntervalMin, ct);

        var now = DateTimeOffset.UtcNow;
        var session = new StageSession
        {
            UserId       = userId,
            StageId      = stageId,
            Token        = Guid.NewGuid().ToString(),
            StartAtMs    = now.ToUnixTimeMilliseconds(),
            IsSetupPhase = true,
            IsExtended   = false
        };

        var ttl = TimeSpan.FromSeconds(stageData.TimeLimit + 300); // timeLimit + 5min buffer
        await _sessionCache.SetAsync(userId, session, ttl, ct);

        return new StageStartResponse
        {
            SessionToken = session.Token,
            ServerStartAt = now.ToString("O")
        };
    }

    public async Task LockAsync(string userId, int stageId, CancellationToken ct)
    {
        var session = await _sessionCache.GetAsync(userId, ct)
            ?? throw new StageSessionNotFoundException();

        if (session.StageId != stageId)
            throw new StageSessionNotFoundException();

        if (!session.IsSetupPhase)
            throw new StageAlreadyLockedException();

        session.IsSetupPhase = false;
        // Reset timer to NOW so server_elapsed_ms is measured from lock (in-game start)
        session.StartAtMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var stageData = _staticData.GetStage(stageId)!;
        var ttl = TimeSpan.FromSeconds(stageData.TimeLimit + 60);
        await _sessionCache.SetAsync(userId, session, ttl, ct);
    }

    public async Task<StageEndResponse> EndAsync(string userId, int stageId, string result, long clientElapsedMs, string correlationId, CancellationToken ct)
    {
        var session = await _sessionCache.GetAsync(userId, ct)
            ?? throw new StageSessionNotFoundException();

        if (session.StageId != stageId)
            throw new StageSessionNotFoundException();

        if (result != "success" && result != "fail")
            throw new InvalidStageResultException();

        await _sessionCache.DeleteAsync(userId, ct);

        if (result == "fail")
        {
            var balance = await _currency.GetBalanceAsync(userId, ct);
            return new StageEndResponse { Score = 0, Stars = 0, AdjustedElapsedMs = 0, IsBestRecord = false, SoftBalanceAfter = balance };
        }

        var stageData = _staticData.GetStage(stageId)!;
        var serverElapsedMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - session.StartAtMs;
        var adjustedMs      = RankingService.AdjustElapsedMs(clientElapsedMs, serverElapsedMs, _config.GetValue<int>("Ranking:NetworkToleranceMs", 2000));
        var score           = RankingService.ComputeScore(stageData.TimeLimit, adjustedMs);
        var maxScore        = stageData.TimeLimit * 100;
        var stars           = score >= maxScore * 3 / 4 ? 3 : score >= maxScore / 2 ? 2 : 1;

        await _progress.UpsertClearAsync(userId, stageId, stars, ct);

        var existing   = await _ranking.GetBestRecordAsync(userId, stageId, ct);
        var isBestRecord = existing == null || adjustedMs < existing.BestClearTimeMs;

        await _rankingService.OnStageClearAsync(userId, stageId, clientElapsedMs, serverElapsedMs, stageData.TimeLimit, ct);

        long softBalance;
        if (stageData.SoftReward > 0)
            softBalance = await _currency.GrantAsync(userId, stageData.SoftReward, "stage_clear", Guid.NewGuid().ToString(), correlationId, ct);
        else
            softBalance = await _currency.GetBalanceAsync(userId, ct);

        return new StageEndResponse
        {
            Score             = score,
            Stars             = stars,
            AdjustedElapsedMs = adjustedMs,
            IsBestRecord      = isBestRecord,
            SoftBalanceAfter  = softBalance
        };
    }

    public async Task ExtendAsync(string userId, int stageId, CancellationToken ct)
    {
        var session = await _sessionCache.GetAsync(userId, ct)
            ?? throw new StageSessionNotFoundException();

        if (session.StageId != stageId)
            throw new StageSessionNotFoundException();

        if (session.IsSetupPhase)
            throw new StageNotInSetupPhaseException();

        if (session.IsExtended)
            throw new StageAlreadyLockedException();

        // Deduct 1 extra stamina to extend
        await _stamina.DeductAsync(userId, MaxStamina, RechargeIntervalMin, ct);

        session.IsExtended = true;

        var stageData = _staticData.GetStage(stageId)!;
        // Extend TTL by the extra time
        var remainingMs = (session.StartAtMs + stageData.TimeLimit * 1000L) - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var newTtl      = TimeSpan.FromMilliseconds(Math.Max(0, remainingMs)) + TimeSpan.FromSeconds(stageData.TimeLimit + 60);
        await _sessionCache.SetAsync(userId, session, newTtl, ct);
    }

    public async Task RecordItemUseAsync(string userId, int stageId, List<ItemUsedEntry> items, CancellationToken ct)
    {
        var session = await _sessionCache.GetAsync(userId, ct)
            ?? throw new StageSessionNotFoundException();

        if (session.StageId != stageId)
            throw new StageSessionNotFoundException();

        if (!session.IsSetupPhase)
            throw new StageNotInSetupPhaseException();

        session.ItemsUsed.AddRange(items);

        var stageData = _staticData.GetStage(stageId)!;
        var ttl = TimeSpan.FromSeconds(stageData.TimeLimit + 300);
        await _sessionCache.SetAsync(userId, session, ttl, ct);
    }
}
