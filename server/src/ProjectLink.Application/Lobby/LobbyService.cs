using ProjectLink.Contracts.Lobby;
using ProjectLink.Domain.Interfaces;

namespace ProjectLink.Application.Lobby;

public class LobbyService
{
    private readonly IUserProfileRepository    _profiles;
    private readonly IStaminaRepository        _stamina;
    private readonly ICurrencyRepository       _currency;
    private readonly IProgressRepository       _progress;
    private readonly IDailyChallengeRepository _dailyChallenge;
    private readonly IStaticDataService        _staticData;

    public LobbyService(
        IUserProfileRepository    profiles,
        IStaminaRepository        stamina,
        ICurrencyRepository       currency,
        IProgressRepository       progress,
        IDailyChallengeRepository dailyChallenge,
        IStaticDataService        staticData)
    {
        _profiles       = profiles;
        _stamina        = stamina;
        _currency       = currency;
        _progress       = progress;
        _dailyChallenge = dailyChallenge;
        _staticData     = staticData;
    }

    public async Task<LobbyStateResponse> GetAsync(string userId, CancellationToken ct)
    {
        var config   = _staticData.GetStaminaConfig();
        var dcConfig = _staticData.GetDailyChallengeConfig();
        var today    = DateOnly.FromDateTime(DateTime.UtcNow);

        var profile  = await _profiles.GetByIdAsync(userId, ct);
        var stamina  = await _stamina.GetComputedAsync(userId, config.MaxStamina, config.RechargeSeconds / 60, ct);
        var balance  = await _currency.GetBalanceAsync(userId, ct);
        var cleared  = (await _progress.GetAllAsync(userId, ct)).ToList();
        var dailyRow = await _dailyChallenge.GetForDateAsync(userId, today, ct);

        // Derive streak: use today's if completed, else check yesterday
        int streakDays;
        if (dailyRow?.Completed == true)
        {
            streakDays = dailyRow.StreakDays;
        }
        else
        {
            var yesterdayRow = await _dailyChallenge.GetForDateAsync(userId, today.AddDays(-1), ct);
            streakDays = (yesterdayRow?.Completed == true) ? yesterdayRow.StreakDays : 0;
        }

        var highestSequential = profile?.MaxClearedStageId ?? 0;
        var totalStars        = cleared.Sum(p => p.Stars);
        var allStages         = _staticData.GetAllStages();
        var maxStageId        = allStages.Count > 0 ? allStages.Max(s => s.StageId) : 0;
        var nextUnlocked      = Math.Min(highestSequential + 1, maxStageId);

        var nextRecharge = stamina.Current < config.MaxStamina
            ? stamina.LastRechargedAt.AddSeconds(config.RechargeSeconds)
            : (DateTimeOffset?)null;

        var playCountToday = dailyRow?.PlayCount ?? 0;
        var completed      = dailyRow?.Completed ?? false;
        var resetAt        = new DateTimeOffset(today.Year, today.Month, today.Day, dcConfig.ResetHourUtc, 0, 0, TimeSpan.Zero)
                                .AddDays(1).ToString("O");

        var activeEvent = _staticData.GetAllSeasonEvents()
            .FirstOrDefault(e =>
            {
                var now = DateTimeOffset.UtcNow;
                return DateTimeOffset.TryParse(e.StartAt, out var start) &&
                       DateTimeOffset.TryParse(e.EndAt,   out var end)   &&
                       now >= start && now < end;
            });

        return new LobbyStateResponse
        {
            Profile = new LobbyProfile
            {
                DisplayName = profile?.DisplayName ?? "",
                AvatarId    = profile?.AvatarId ?? 1,
            },
            Stamina = new LobbyStamina
            {
                Current        = stamina.Current,
                Max            = config.MaxStamina,
                NextRechargeAt = nextRecharge?.ToString("O"),
            },
            Currency = new LobbyCurrency
            {
                SoftAmount = balance,
            },
            ProgressSummary = new LobbyProgressSummary
            {
                HighestStageId      = highestSequential,
                TotalStarsEarned    = totalStars,
                NextUnlockedStageId = nextUnlocked,
            },
            DailyChallenge = new LobbyDailyChallenge
            {
                CompletedToday  = completed,
                CanComplete     = !completed && playCountToday >= dcConfig.PlayCountTarget,
                PlayCountToday  = playCountToday,
                PlayCountTarget = dcConfig.PlayCountTarget,
                StreakDays      = streakDays,
                ResetAt         = resetAt,
            },
            SeasonEvent = activeEvent is null ? null : new LobbySeasonEvent
            {
                EventId  = activeEvent.EventId,
                Name     = activeEvent.Name,
                EndAt    = activeEvent.EndAt,
                IsActive = true,
            },
        };
    }
}
