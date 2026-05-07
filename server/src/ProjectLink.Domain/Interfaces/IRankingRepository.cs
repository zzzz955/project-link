using ProjectLink.Domain.Entities;

namespace ProjectLink.Domain.Interfaces;

public interface IRankingRepository
{
    Task UpsertBestRecordAsync(string userId, int stageId, long clearTimeMs, int score, CancellationToken ct);
    Task<StageBestRecord?> GetBestRecordAsync(string userId, int stageId, CancellationToken ct);
    Task<List<StageBestRecord>> GetUserBestRecordsAsync(string userId, CancellationToken ct);
    Task<List<StageBestRecord>> GetAllBestRecordsAsync(CancellationToken ct);
    Task<List<UserRankingCache>> GetAllRankingCachesAsync(CancellationToken ct);
    Task UpsertRankingCacheAsync(string userId, long totalScore, int stagesCleared, CancellationToken ct);
}
