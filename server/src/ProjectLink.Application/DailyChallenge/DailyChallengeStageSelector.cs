using ProjectLink.Domain.StaticData;

namespace ProjectLink.Application.DailyChallenge;

internal static class DailyChallengeStageSelector
{
    internal static List<int> GetTodayStageIds(int stagePickCount, IReadOnlyList<IngameStageData> allStages)
    {
        if (allStages.Count == 0) return new List<int>();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var rng = new Random(today.DayNumber);
        return allStages
            .Select(s => s.StageId)
            .OrderBy(_ => rng.Next())
            .Take(Math.Min(stagePickCount, allStages.Count))
            .ToList();
    }
}
