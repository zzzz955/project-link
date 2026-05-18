using ProjectLink.Contracts.StreakChallenge;
using ProjectLink.Domain.Entities;
using ProjectLink.Domain.Interfaces;
using ProjectLink.Domain.StaticData;
using ProjectLink.Domain.Utilities;

namespace ProjectLink.Application.StreakChallenge;

public class StreakChallengeService
{
    const int DefaultEventId    = 1;
    const int AdMultiplierValue = 2;

    private readonly IStreakChallengeRepository _repo;
    private readonly IStreakChallengeTransaction _tx;
    private readonly IStaticDataService          _staticData;

    public StreakChallengeService(
        IStreakChallengeRepository repo,
        IStreakChallengeTransaction tx,
        IStaticDataService         staticData)
    {
        _repo       = repo;
        _tx         = tx;
        _staticData = staticData;
    }

    // ── entry points ─────────────────────────────────────────────────────────

    public async Task<StreakChallengeStateResponse> GetStateAsync(string userId, CancellationToken ct)
    {
        var (state, levels) = await LoadAndLazyResetAsync(userId, DefaultEventId, ct);
        if (state is null)
        {
            var ev   = _staticData.GetLatestEnabledStreakChallengeEvent();
            var defs = ev != null ? _staticData.GetStreakChallengeLevels(ev.EventId, ev.Version) : null;
            return BuildStateResponse(null, new List<StreakChallengeUserLevelState>(), levelDefs: defs);
        }
        return BuildStateResponse(state, levels);
    }

    public async Task<StreakChallengeStateResponse> ActivateAsync(string userId, CancellationToken ct)
    {
        var (state, levels) = await LoadAndLazyResetAsync(userId, DefaultEventId, ct);

        if (state is { EventStatus: "ACTIVE" or "COMPLETED" })
            return BuildStateResponse(state, levels);

        var eventData = _staticData.GetLatestEnabledStreakChallengeEvent()
            ?? throw new InvalidOperationException("STREAK_EVENT_UNAVAILABLE");

        var levelDefs = _staticData.GetStreakChallengeLevels(eventData.EventId, eventData.Version)
            .Where(l => l.IsEnabled)
            .OrderBy(l => l.DisplayOrder)
            .ToList();

        if (levelDefs.Count == 0)
            throw new InvalidOperationException("STREAK_NO_LEVELS");

        var now      = DateTimeOffset.UtcNow;
        var cycleId  = IdHelper.NewId();
        var expiresAt = now.AddSeconds(eventData.DurationSeconds);

        await _tx.ActivateAsync(new StreakChallengeActivateCommand
        {
            UserId       = userId,
            EventId      = eventData.EventId,
            CycleId      = cycleId,
            EventVersion = eventData.Version,
            ActivatedAt  = now,
            ExpiresAt    = expiresAt,
            Levels       = levelDefs,
        }, ct);

        var (newState, newLevels) = await LoadStateRawAsync(userId, eventData.EventId, cycleId, ct);
        return BuildStateResponse(newState, newLevels);
    }

    public async Task<StreakChallengeStateResponse> StartLevelAsync(string userId, int levelIndex, CancellationToken ct)
    {
        var (state, levels) = await LoadAndLazyResetAsync(userId, DefaultEventId, ct);

        if (state is null || state.EventStatus != "ACTIVE")
            return BuildStateResponse(state, levels, exclusion: "EVENT_INACTIVE");

        var level = levels.FirstOrDefault(l => l.LevelIndex == levelIndex);
        if (level is null)
            return BuildStateResponse(state, levels, exclusion: "INVALID_EVENT_STATE");

        if (level.LevelStatus == "STARTED")
            return BuildStateResponse(state, levels);

        if (level.LevelStatus != "READY")
            return BuildStateResponse(state, levels, exclusion: "INVALID_EVENT_STATE");

        if (levels.Any(l => l.RewardState == "PENDING"))
            return BuildStateResponse(state, levels, exclusion: "PENDING_REWARD_EXISTS");

        await _tx.StartLevelAsync(new StreakChallengeStartLevelCommand
        {
            UserId     = userId,
            EventId    = state.EventId,
            CycleId    = state.CycleId,
            LevelIndex = levelIndex,
        }, ct);

        var (newState, newLevels) = await LoadStateRawAsync(userId, state.EventId, state.CycleId, ct);
        return BuildStateResponse(newState, newLevels);
    }

    public async Task<StreakChallengeStageResultResponse> ProcessStageResultAsync(
        string userId, int stageId, bool isFirstClear, bool isMainStage,
        string result, CancellationToken ct)
    {
        var (state, levels) = await LoadAndLazyResetAsync(userId, DefaultEventId, ct);

        if (state is null || state.EventStatus != "ACTIVE")
            return new StreakChallengeStageResultResponse { Counted = false, ExclusionReason = "EVENT_INACTIVE" };

        var currentLevel = levels.FirstOrDefault(l => l.LevelIndex == state.CurrentLevel);
        if (currentLevel is null || currentLevel.LevelStatus != "STARTED")
            return new StreakChallengeStageResultResponse { Counted = false, ExclusionReason = "LEVEL_NOT_STARTED" };

        if (!isMainStage)
            return new StreakChallengeStageResultResponse { Counted = false, ExclusionReason = "NOT_MAIN_STAGE" };

        if (result == "success" && !isFirstClear)
            return new StreakChallengeStageResultResponse { Counted = false, ExclusionReason = "ALREADY_CLEARED" };

        if (result == "success" && state.LastCountedStageId == stageId)
            return new StreakChallengeStageResultResponse { Counted = false, ExclusionReason = "DUPLICATE_RESULT" };

        var progressResult = await _tx.RecordProgressAsync(new StreakChallengeProgressCommand
        {
            UserId     = userId,
            EventId    = state.EventId,
            CycleId    = state.CycleId,
            LevelIndex = state.CurrentLevel,
            StageId    = stageId,
            IsSuccess  = result == "success",
        }, ct);

        var (newState, newLevels) = await LoadStateRawAsync(userId, state.EventId, state.CycleId, ct);
        var eventState            = BuildStateResponse(newState, newLevels);

        string directive    = "NONE";
        int    directiveLevel = state.CurrentLevel;

        if (result == "success" && progressResult.LevelCompleted)
        {
            directive     = "RETURN_TO_LOBBY";
            directiveLevel = state.CurrentLevel;
            eventState.NavigationDirective = "OPEN_REWARD_POPUP";
            eventState.NavigationLevel     = state.CurrentLevel;
        }
        else if (result == "fail")
        {
            directive      = "OPEN_FAILURE_POPUP";
            directiveLevel = state.CurrentLevel;
        }

        return new StreakChallengeStageResultResponse
        {
            Counted             = result == "success",
            EventState          = eventState,
            NavigationDirective = directive,
            NavigationLevel     = directiveLevel,
        };
    }

    public async Task<StreakChallengeClaimRewardResponse> ClaimRewardAsync(
        string userId, int levelIndex, string correlationId, CancellationToken ct)
    {
        var (state, levels) = await LoadAndLazyResetAsync(userId, DefaultEventId, ct);

        if (state is null || state.EventStatus is "INACTIVE" or "EXPIRED")
            throw new InvalidOperationException("STREAK_EVENT_INACTIVE");

        var level = levels.FirstOrDefault(l => l.LevelIndex == levelIndex)
            ?? throw new InvalidOperationException("STREAK_INVALID_LEVEL");

        if (level.RewardState == "CLAIMED")
        {
            var (s2, l2) = await LoadStateRawAsync(userId, state.EventId, state.CycleId, ct);
            return new StreakChallengeClaimRewardResponse
            {
                EventState = BuildStateResponse(s2, l2),
            };
        }

        if (level.RewardState != "PENDING")
            throw new InvalidOperationException("STREAK_REWARD_NOT_PENDING");

        var levelDef = _staticData.GetStreakChallengeLevels(state.EventId, state.EventVersion)
            .FirstOrDefault(l => l.LevelIndex == levelIndex)
            ?? throw new InvalidOperationException("STREAK_METADATA_UNAVAILABLE");

        var rewardItems = _staticData.GetStreakChallengeRewardItems(levelDef.RewardGroupId, 1);

        var claimResult = await _tx.ClaimRewardAsync(new StreakChallengeClaimCommand
        {
            UserId             = userId,
            EventId            = state.EventId,
            CycleId            = state.CycleId,
            LevelIndex         = levelIndex,
            RewardGroupId      = levelDef.RewardGroupId,
            RewardGroupVersion = 1,
            RewardMultiplier   = 1,
            CorrelationId      = correlationId,
            RewardItems        = rewardItems.ToList(),
        }, ct);

        var (newState, newLevels) = await LoadStateRawAsync(userId, state.EventId, state.CycleId, ct);
        var eventState            = BuildStateResponse(newState, newLevels);
        eventState.NavigationDirective = DeterminePostClaimDirective(newState, newLevels, levelIndex);
        eventState.NavigationLevel     = levelIndex;

        return new StreakChallengeClaimRewardResponse
        {
            RewardsGranted    = rewardItems.Select(r => new StreakChallengeRewardItem
            {
                ItemType = r.ItemType,
                ItemId   = r.ItemId,
                Amount   = r.Amount,
            }).ToList(),
            SoftBalanceAfter  = claimResult.SoftBalanceAfter,
            InventoryUpdates  = claimResult.InventoryAfter
                .Select(kv => new StreakChallengeInventoryUpdate { ItemId = kv.Key, QuantityAfter = kv.Value })
                .ToList(),
            EventState        = eventState,
            MultiplierApplied = false,
        };
    }

    public async Task<StreakChallengeClaimRewardResponse> ClaimRewardWithAdAsync(
        string userId, int levelIndex, string adToken, string adPlacementId, string correlationId, CancellationToken ct)
    {
        var (state, levels) = await LoadAndLazyResetAsync(userId, DefaultEventId, ct);

        if (state is null || state.EventStatus is "INACTIVE" or "EXPIRED")
            throw new InvalidOperationException("STREAK_EVENT_INACTIVE");

        var level = levels.FirstOrDefault(l => l.LevelIndex == levelIndex)
            ?? throw new InvalidOperationException("STREAK_INVALID_LEVEL");

        if (level.RewardState != "PENDING")
            throw new InvalidOperationException("STREAK_REWARD_NOT_PENDING");

        var adAlreadyUsed = await _repo.HasAdUsedForLevelAsync(userId, state.EventId, state.CycleId, levelIndex, ct);
        if (adAlreadyUsed)
            throw new InvalidOperationException("STREAK_AD_ALREADY_USED");

        await _tx.RecordAdAsync(new StreakChallengeAdRewardHistory
        {
            UserId           = userId,
            EventId          = state.EventId,
            CycleId          = state.CycleId,
            LevelIndex       = levelIndex,
            AdPlacementId    = adPlacementId,
            AdResult         = "SUCCESS",
            MultiplierApplied = true,
            CreatedAt        = DateTimeOffset.UtcNow,
        }, ct);

        var levelDef = _staticData.GetStreakChallengeLevels(state.EventId, state.EventVersion)
            .FirstOrDefault(l => l.LevelIndex == levelIndex)
            ?? throw new InvalidOperationException("STREAK_METADATA_UNAVAILABLE");

        var rewardItems = _staticData.GetStreakChallengeRewardItems(levelDef.RewardGroupId, 1);

        var claimResult = await _tx.ClaimRewardAsync(new StreakChallengeClaimCommand
        {
            UserId             = userId,
            EventId            = state.EventId,
            CycleId            = state.CycleId,
            LevelIndex         = levelIndex,
            RewardGroupId      = levelDef.RewardGroupId,
            RewardGroupVersion = 1,
            RewardMultiplier   = AdMultiplierValue,
            CorrelationId      = correlationId,
            RewardItems        = rewardItems.ToList(),
        }, ct);

        var (newState, newLevels) = await LoadStateRawAsync(userId, state.EventId, state.CycleId, ct);
        var eventState            = BuildStateResponse(newState, newLevels);
        eventState.NavigationDirective = DeterminePostClaimDirective(newState, newLevels, levelIndex);
        eventState.NavigationLevel     = levelIndex;

        return new StreakChallengeClaimRewardResponse
        {
            RewardsGranted    = rewardItems.Select(r => new StreakChallengeRewardItem
            {
                ItemType = r.ItemType,
                ItemId   = r.ItemId,
                Amount   = r.Amount * AdMultiplierValue,
            }).ToList(),
            SoftBalanceAfter  = claimResult.SoftBalanceAfter,
            InventoryUpdates  = claimResult.InventoryAfter
                .Select(kv => new StreakChallengeInventoryUpdate { ItemId = kv.Key, QuantityAfter = kv.Value })
                .ToList(),
            EventState        = eventState,
            MultiplierApplied = true,
        };
    }

    public async Task<LobbyStreakChallengeSnapshot> GetLobbySnapshotAsync(string userId, CancellationToken ct)
    {
        var (state, levels) = await LoadAndLazyResetAsync(userId, DefaultEventId, ct);
        if (state is null || state.EventStatus == "INACTIVE")
            return new LobbyStreakChallengeSnapshot { EventStatus = "INACTIVE" };

        var currentLevel = levels.FirstOrDefault(l => l.LevelIndex == state.CurrentLevel);
        var hasPending   = levels.Any(l => l.RewardState == "PENDING");

        var remaining = state.EventStatus == "ACTIVE"
            ? state.ExpiresAt - DateTimeOffset.UtcNow
            : TimeSpan.Zero;

        return new LobbyStreakChallengeSnapshot
        {
            EventStatus          = state.EventStatus,
            RemainingTimeIso     = remaining > TimeSpan.Zero ? System.Xml.XmlConvert.ToString(remaining) : "",
            CurrentLevel         = state.CurrentLevel,
            CurrentLevelCount    = currentLevel?.CurrentCount ?? 0,
            CurrentLevelRequired = currentLevel?.RequiredCount ?? 0,
            HasPendingReward     = hasPending,
        };
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private async Task<(StreakChallengeUserState? state, List<StreakChallengeUserLevelState> levels)>
        LoadAndLazyResetAsync(string userId, int eventId, CancellationToken ct)
    {
        var state = await _repo.GetActiveStateAsync(userId, eventId, ct);
        if (state is null)
            return (null, new List<StreakChallengeUserLevelState>());

        if (state.EventStatus == "ACTIVE" && DateTimeOffset.UtcNow >= state.ExpiresAt)
        {
            await _tx.LazyResetAsync(new StreakChallengeLazyResetCommand
            {
                UserId    = userId,
                EventId   = eventId,
                CycleId   = state.CycleId,
                ExpiredAt = DateTimeOffset.UtcNow,
            }, ct);

            state = await _repo.GetActiveStateAsync(userId, eventId, ct);
        }

        if (state is null)
            return (null, new List<StreakChallengeUserLevelState>());

        var levels = await _repo.GetLevelStatesAsync(userId, eventId, state.CycleId, ct);
        return (state, levels);
    }

    private async Task<(StreakChallengeUserState? state, List<StreakChallengeUserLevelState> levels)>
        LoadStateRawAsync(string userId, int eventId, string cycleId, CancellationToken ct)
    {
        var state = await _repo.GetActiveStateAsync(userId, eventId, ct);
        if (state is null) return (null, new List<StreakChallengeUserLevelState>());
        var levels = await _repo.GetLevelStatesAsync(userId, eventId, cycleId, ct);
        return (state, levels);
    }

    private static StreakChallengeStateResponse BuildStateResponse(
        StreakChallengeUserState?                state,
        List<StreakChallengeUserLevelState>      levels,
        string?                                  exclusion = null,
        IReadOnlyList<StreakChallengeLevelData>? levelDefs = null)
    {
        if (state is null)
        {
            var placeholder = levelDefs != null
                ? levelDefs
                    .OrderBy(l => l.LevelIndex)
                    .Select(l => new StreakChallengeLevelState
                    {
                        LevelIndex    = l.LevelIndex,
                        LevelStatus   = "LOCKED",
                        RequiredCount = l.RequiredClearCount,
                        CurrentCount  = 0,
                        RewardState   = "NONE",
                    })
                    .ToList()
                : new List<StreakChallengeLevelState>();

            return new StreakChallengeStateResponse
            {
                EventStatus      = "INACTIVE",
                Levels           = placeholder,
                AvailableActions = new List<string> { "ACTIVATE" },
                ExclusionReason  = exclusion,
            };
        }

        var remaining = state.EventStatus == "ACTIVE"
            ? state.ExpiresAt - DateTimeOffset.UtcNow
            : TimeSpan.Zero;

        var levelStates = levels.Select(l => new StreakChallengeLevelState
        {
            LevelIndex    = l.LevelIndex,
            LevelStatus   = l.LevelStatus,
            RequiredCount = l.RequiredCount,
            CurrentCount  = l.CurrentCount,
            RewardState   = l.RewardState,
        }).OrderBy(l => l.LevelIndex).ToList();

        var actions    = DetermineAvailableActions(state, levels);
        var directive  = DetermineNavigationDirective(state, levels);
        var adUsed     = false;

        return new StreakChallengeStateResponse
        {
            EventId             = state.EventId,
            EventStatus         = state.EventStatus,
            RemainingTimeIso    = remaining > TimeSpan.Zero ? System.Xml.XmlConvert.ToString(remaining) : "",
            ExpiresAtIso        = state.ExpiresAt.ToString("O"),
            CycleId             = state.CycleId,
            EventVersion        = state.EventVersion,
            CurrentLevel        = state.CurrentLevel,
            Levels              = levelStates,
            AvailableActions    = actions,
            NavigationDirective = directive,
            NavigationLevel     = state.CurrentLevel,
            ExclusionReason     = exclusion,
            AdMultiplier        = AdMultiplierValue,
            AdUsedThisLevel     = adUsed,
        };
    }

    private static List<string> DetermineAvailableActions(
        StreakChallengeUserState            state,
        List<StreakChallengeUserLevelState> levels)
    {
        var actions = new List<string>();
        switch (state.EventStatus)
        {
            case "INACTIVE":
            case "EXPIRED":
                actions.Add("ACTIVATE");
                break;
            case "ACTIVE":
                var current        = levels.FirstOrDefault(l => l.LevelIndex == state.CurrentLevel);
                var hasPendingReward = levels.Any(l => l.RewardState == "PENDING");
                if (!hasPendingReward && current?.LevelStatus == "READY")   actions.Add("START_LEVEL");
                if (current?.LevelStatus == "STARTED")                      actions.Add("CONTINUE_LEVEL");
                if (hasPendingReward)
                {
                    actions.Add("CLAIM_REWARD");
                    actions.Add("WATCH_AD");
                }
                break;
            case "COMPLETED":
                actions.Add("VIEW_COMPLETED");
                break;
        }
        return actions;
    }

    private static string DetermineNavigationDirective(
        StreakChallengeUserState            state,
        List<StreakChallengeUserLevelState> levels)
    {
        if (state.EventStatus == "EXPIRED")   return "OPEN_EXPIRATION_POPUP";
        if (state.EventStatus == "COMPLETED") return "OPEN_EVENT_POPUP";
        if (levels.Any(l => l.RewardState == "PENDING")) return "OPEN_REWARD_POPUP";
        if (levels.Any(l => l.LevelStatus == "READY"))   return "OPEN_LEVEL_START_POPUP";
        return "NONE";
    }

    private static string DeterminePostClaimDirective(
        StreakChallengeUserState?           state,
        List<StreakChallengeUserLevelState> levels,
        int                                 claimedLevel)
    {
        if (state is null) return "NONE";
        if (state.EventStatus == "COMPLETED") return "OPEN_EVENT_POPUP";
        var nextLevel = levels.FirstOrDefault(l => l.LevelIndex == claimedLevel + 1);
        if (nextLevel?.LevelStatus == "READY") return "OPEN_LEVEL_START_POPUP";
        return "NONE";
    }
}

public class LobbyStreakChallengeSnapshot
{
    public string EventStatus          { get; set; } = "INACTIVE";
    public string RemainingTimeIso     { get; set; } = "";
    public int    CurrentLevel         { get; set; }
    public int    CurrentLevelCount    { get; set; }
    public int    CurrentLevelRequired { get; set; }
    public bool   HasPendingReward     { get; set; }
}
