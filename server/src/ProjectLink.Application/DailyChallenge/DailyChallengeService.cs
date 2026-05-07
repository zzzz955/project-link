using ProjectLink.Contracts.Daily;
using ProjectLink.Domain.Exceptions;
using ProjectLink.Domain.Interfaces;

namespace ProjectLink.Application.DailyChallenge;

public class DailyChallengeService
{
    private readonly IDailyChallengeRepository          _repo;
    private readonly IDailyChallengeCompleteTransaction _completeTx;
    private readonly IStaticDataService                 _staticData;

    public DailyChallengeService(
        IDailyChallengeRepository          repo,
        IDailyChallengeCompleteTransaction completeTx,
        IStaticDataService                 staticData)
    {
        _repo       = repo;
        _completeTx = completeTx;
        _staticData = staticData;
    }

    // Returns the current running streak (before today's potential completion).
    // If yesterday was completed consecutively, returns yesterday.StreakDays.
    // Otherwise returns 0.
    private async Task<int> GetCurrentStreakAsync(string userId, DateOnly today, CancellationToken ct)
    {
        var yesterday    = today.AddDays(-1);
        var yesterdayRow = await _repo.GetForDateAsync(userId, yesterday, ct);
        return (yesterdayRow?.Completed == true) ? yesterdayRow.StreakDays : 0;
    }

    public async Task<DailyChallengeResponse> GetAsync(string userId, CancellationToken ct)
    {
        var config = _staticData.GetDailyChallengeConfig();
        var today  = DateOnly.FromDateTime(DateTime.UtcNow);
        var row    = await _repo.GetForDateAsync(userId, today, ct);

        var playCountToday = row?.PlayCount ?? 0;
        var completed      = row?.Completed ?? false;

        // If today is already completed, use today's streak. Otherwise derive from yesterday.
        int streakDays;
        if (completed)
            streakDays = row!.StreakDays;
        else
            streakDays = await GetCurrentStreakAsync(userId, today, ct);

        var resetAt = new DateTimeOffset(today.Year, today.Month, today.Day, config.ResetHourUtc, 0, 0, TimeSpan.Zero)
                          .AddDays(1).ToString("O");

        // Build 7-day streak tiles based on current streak position
        var positionInCycle = streakDays % 7; // 0-indexed position AFTER the last completed day
        var tiles = new List<DailyChallengeStreakTile>();
        for (var day = 1; day <= 7; day++)
        {
            var tileIndex = day - 1;
            tiles.Add(new DailyChallengeStreakTile
            {
                Day      = day,
                IsDone   = tileIndex < positionInCycle,
                IsToday  = tileIndex == positionInCycle && !completed,
                IsLocked = tileIndex > positionInCycle && !completed,
            });
        }

        // Today's reward: the next streak day
        var todayStreakDay = positionInCycle + 1;
        var todayReward   = _staticData.GetDailyReward(todayStreakDay);
        var todayRewards  = new List<DailyChallengeRewardPreview>();
        if (todayReward != null)
        {
            todayRewards.Add(new DailyChallengeRewardPreview
            {
                RewardType = todayReward.RewardType,
                RewardId   = todayReward.RewardId,
                Amount     = todayReward.Amount,
            });
        }

        return new DailyChallengeResponse
        {
            CompletedToday  = completed,
            CanComplete     = !completed && playCountToday >= config.PlayCountTarget,
            PlayCountToday  = playCountToday,
            PlayCountTarget = config.PlayCountTarget,
            StreakDays      = streakDays,
            ResetAt         = resetAt,
            Tiles           = tiles,
            TodayRewards    = todayRewards,
        };
    }

    public async Task<DailyChallengeCompleteResponse> CompleteAsync(string userId, string correlationId, CancellationToken ct)
    {
        var config = _staticData.GetDailyChallengeConfig();
        var today  = DateOnly.FromDateTime(DateTime.UtcNow);
        var row    = await _repo.GetForDateAsync(userId, today, ct);

        if (row is null || row.PlayCount < config.PlayCountTarget)
            throw new DailyChallengeNotCompletableException();

        if (row.Completed)
            throw new DailyChallengeAlreadyCompletedException();

        var currentStreak = await GetCurrentStreakAsync(userId, today, ct);
        var todayStreakDay = (currentStreak % 7) + 1;
        var reward        = _staticData.GetDailyReward(todayStreakDay);

        var rewards = new List<DailyChallengeRewardInput>();
        if (reward != null)
        {
            rewards.Add(new DailyChallengeRewardInput
            {
                RewardType = reward.RewardType,
                RewardId   = reward.RewardId,
                Amount     = reward.Amount,
            });
        }

        var dbResult = await _completeTx.ExecuteAsync(userId, today, currentStreak, rewards, correlationId, ct);

        return new DailyChallengeCompleteResponse
        {
            RewardsGranted = rewards.Select(r => new DailyChallengeRewardGranted
            {
                RewardType = r.RewardType,
                RewardId   = r.RewardId,
                Amount     = r.Amount,
            }).ToList(),
            StreakDays       = dbResult.NewStreakDays,
            SoftBalanceAfter = dbResult.SoftBalanceAfter,
            InventoryUpdates = dbResult.InventoryAfter
                .Select(kv => new DailyInventoryUpdate { ItemId = kv.Key, QuantityAfter = kv.Value })
                .ToList(),
        };
    }
}
