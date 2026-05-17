using ProjectLink.Contracts.Ranking;
using ProjectLink.Domain.Interfaces;
using StackExchange.Redis;

namespace ProjectLink.Application.Ranking;

public class RankingService
{
    private readonly IRankingRepository   _repo;
    private readonly IUserProfileRepository _profiles;
    private readonly IDatabase            _redis;
    private readonly IConfiguration       _config;

    private const long MaxUnixTs = 9_999_999_999L;

    public RankingService(IRankingRepository repo, IUserProfileRepository profiles, IConnectionMultiplexer redis, IConfiguration config)
    {
        _repo     = repo;
        _profiles = profiles;
        _redis    = redis.GetDatabase();
        _config   = config;
    }

    public int NetworkToleranceMs => _config.GetValue<int>("Ranking:NetworkToleranceMs", 2000);

    public static int ComputeScore(int timeLimit, long adjustedElapsedMs)
    {
        var elapsedCs = (int)(adjustedElapsedMs / 10);
        return Math.Max(0, timeLimit * 100 - elapsedCs);
    }

    public static long AdjustElapsedMs(long clientElapsedMs, long serverElapsedMs, int toleranceMs)
        => Math.Max(clientElapsedMs, serverElapsedMs - toleranceMs);

    // Encodes score for descending ranking (higher is better)
    private static double EncodeDescending(long primaryValue, long accountCreatedAtUnix)
        => primaryValue * 1e10 + (MaxUnixTs - accountCreatedAtUnix);

    // Encodes elapsed_cs for ascending ranking (lower is better)
    private static double EncodeAscending(long elapsedCs, long accountCreatedAtUnix)
        => elapsedCs * 1e10 + accountCreatedAtUnix;

    // Called after IStageEndTransaction commits — updates Redis from pre-computed DB values
    public async Task<object?> OnStageEndAsync(string userId, int stageId, long adjustedMs, int score, long totalScore, int stagesCleared, CancellationToken ct)
    {
        var profile            = await _profiles.GetByIdAsync(userId, ct);
        var accountCreatedUnix = profile?.AccountCreatedAt.ToUnixTimeSeconds() ?? 0L;

        await _redis.SortedSetAddAsync($"ranking:stage:{stageId}:score", userId, EncodeDescending(score, accountCreatedUnix));
        await _redis.SortedSetAddAsync("ranking:global:stages", userId, EncodeDescending(stagesCleared, accountCreatedUnix));
        await _redis.SortedSetAddAsync("ranking:global:score",  userId, EncodeDescending(totalScore,    accountCreatedUnix));
        return null;
    }

    public async Task OnStageClearAsync(string userId, int stageId, long clientElapsedMs, long serverElapsedMs, int timeLimit, CancellationToken ct)
    {
        var adjustedMs = AdjustElapsedMs(clientElapsedMs, serverElapsedMs, NetworkToleranceMs);
        var score      = ComputeScore(timeLimit, adjustedMs);

        var existing = await _repo.GetBestRecordAsync(userId, stageId, ct);
        if (existing != null && existing.BestScore >= score)
            return; // not a new best

        await _repo.UpsertBestRecordAsync(userId, stageId, adjustedMs, score, ct);

        var allRecords    = await _repo.GetUserBestRecordsAsync(userId, ct);
        var totalScore    = (long)allRecords.Sum(r => (long)r.BestScore);
        var stagesCleared = allRecords.Count;

        await _repo.UpsertRankingCacheAsync(userId, totalScore, stagesCleared, ct);

        var profile            = await _profiles.GetByIdAsync(userId, ct);
        var accountCreatedUnix = profile?.AccountCreatedAt.ToUnixTimeSeconds() ?? 0L;

        await _redis.SortedSetAddAsync($"ranking:stage:{stageId}:score", userId, EncodeDescending(score, accountCreatedUnix));
        await _redis.SortedSetAddAsync("ranking:global:stages", userId, EncodeDescending(stagesCleared, accountCreatedUnix));
        await _redis.SortedSetAddAsync("ranking:global:score",  userId, EncodeDescending(totalScore, accountCreatedUnix));
    }

    public async Task<RankingListResponse> GetStageRankingAsync(int stageId, int top, string? callerId, CancellationToken ct)
    {
        var key     = $"ranking:stage:{stageId}:score";
        var entries = await _redis.SortedSetRangeByRankWithScoresAsync(key, 0, top - 1, Order.Descending);
        return await BuildListResponseAsync(entries, isAscending: false, callerId, key, "STAGE_SCORE", "Best Score", ct);
    }

    public async Task<RankingListResponse> GetGlobalStagesRankingAsync(int top, string? callerId, CancellationToken ct)
    {
        var key     = "ranking:global:stages";
        var entries = await _redis.SortedSetRangeByRankWithScoresAsync(key, 0, top - 1, Order.Descending);
        return await BuildListResponseAsync(entries, isAscending: false, callerId, key, "GLOBAL_STAGES", "Stages Cleared", ct);
    }

    public async Task<RankingListResponse> GetGlobalScoreRankingAsync(int top, string? callerId, CancellationToken ct)
    {
        var key     = "ranking:global:score";
        var entries = await _redis.SortedSetRangeByRankWithScoresAsync(key, 0, top - 1, Order.Descending);
        return await BuildListResponseAsync(entries, isAscending: false, callerId, key, "GLOBAL_SCORE", "Total Score", ct);
    }

    private async Task<RankingListResponse> BuildListResponseAsync(SortedSetEntry[] entries, bool isAscending, string? callerId, string key, string category, string metricLabel, CancellationToken ct)
    {
        var rankEntries = new List<RankingEntry>();
        for (var i = 0; i < entries.Length; i++)
        {
            var userId  = (string)entries[i].Element!;
            var profile = await _profiles.GetByIdAsync(userId, ct);
            var value   = DecodeValue(entries[i].Score, isAscending);
            rankEntries.Add(new RankingEntry
            {
                Rank        = i + 1,
                UserId      = userId,
                DisplayName = profile?.DisplayName ?? "",
                AvatarId    = profile?.AvatarId ?? 1,
                IsMe        = userId == callerId,
                Value       = value,
            });
        }

        RankingEntry? myRank = null;
        if (callerId != null)
        {
            var rank = isAscending
                ? await _redis.SortedSetRankAsync(key, callerId)
                : await _redis.SortedSetRankAsync(key, callerId, Order.Descending);

            if (rank.HasValue)
            {
                var score   = await _redis.SortedSetScoreAsync(key, callerId);
                var profile = await _profiles.GetByIdAsync(callerId, ct);
                myRank = new RankingEntry
                {
                    Rank        = (int)rank.Value + 1,
                    UserId      = callerId,
                    DisplayName = profile?.DisplayName ?? "",
                    AvatarId    = profile?.AvatarId ?? 1,
                    IsMe        = true,
                    Value       = DecodeValue(score!.Value, isAscending),
                };
            }
        }

        return new RankingListResponse
        {
            Entries     = rankEntries,
            MyRank      = myRank,
            Category    = category,
            MetricLabel = metricLabel,
        };
    }

    private static long DecodeValue(double encodedScore, bool isAscending)
    {
        // Strip the tie-break portion (last 10 decimal digits worth)
        if (isAscending)
            return (long)(encodedScore / 1e10);
        // For descending: primary = floor(encoded / 1e10)
        return (long)(encodedScore / 1e10);
    }

    public async Task<int?> GetStageRankPercentileAsync(string userId, int stageId, CancellationToken ct)
    {
        var key   = $"ranking:stage:{stageId}:score";
        var rank  = await _redis.SortedSetRankAsync(key, userId, Order.Descending);
        if (!rank.HasValue) return null;
        var total = await _redis.SortedSetLengthAsync(key);
        if (total == 0) return null;
        return (int)Math.Ceiling((double)(rank.Value + 1) / total * 100);
    }

    public async Task<MyRankResponse> GetMyRankAsync(string userId, CancellationToken ct)
    {
        MyRankEntry? stagesEntry = null;
        MyRankEntry? scoreEntry  = null;

        var stagesRank = await _redis.SortedSetRankAsync("ranking:global:stages", userId, Order.Descending);
        if (stagesRank.HasValue)
        {
            var score  = await _redis.SortedSetScoreAsync("ranking:global:stages", userId);
            stagesEntry = new MyRankEntry { Rank = (int)stagesRank.Value + 1, Value = DecodeValue(score!.Value, false) };
        }

        var scoreRank = await _redis.SortedSetRankAsync("ranking:global:score", userId, Order.Descending);
        if (scoreRank.HasValue)
        {
            var score = await _redis.SortedSetScoreAsync("ranking:global:score", userId);
            scoreEntry = new MyRankEntry { Rank = (int)scoreRank.Value + 1, Value = DecodeValue(score!.Value, false) };
        }

        return new MyRankResponse { StagesCleared = stagesEntry, TotalScore = scoreEntry };
    }

    public async Task RebuildFromDbAsync(CancellationToken ct)
    {
        var records  = await _repo.GetAllBestRecordsAsync(ct);
        var caches   = await _repo.GetAllRankingCachesAsync(ct);
        var cacheMap = caches.ToDictionary(c => c.UserId);

        // Per-stage score rankings
        foreach (var record in records)
        {
            var profile            = await _profiles.GetByIdAsync(record.UserId, ct);
            var accountCreatedUnix = profile?.AccountCreatedAt.ToUnixTimeSeconds() ?? 0L;
            var stageKey           = $"ranking:stage:{record.StageId}:score";
            await _redis.SortedSetAddAsync(stageKey, record.UserId, EncodeDescending(record.BestScore, accountCreatedUnix));
        }

        // Global rankings
        foreach (var cache in caches)
        {
            var profile           = await _profiles.GetByIdAsync(cache.UserId, ct);
            var accountCreatedUnix = profile?.AccountCreatedAt.ToUnixTimeSeconds() ?? 0L;
            await _redis.SortedSetAddAsync("ranking:global:stages", cache.UserId, EncodeDescending(cache.StagesCleared, accountCreatedUnix));
            await _redis.SortedSetAddAsync("ranking:global:score",  cache.UserId, EncodeDescending(cache.TotalScore, accountCreatedUnix));
        }
    }
}
