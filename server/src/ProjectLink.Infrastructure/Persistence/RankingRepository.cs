using Microsoft.EntityFrameworkCore;
using ProjectLink.Domain.Entities;
using ProjectLink.Domain.Interfaces;

namespace ProjectLink.Infrastructure.Persistence;

public class RankingRepository : IRankingRepository
{
    private readonly AppDbContext _db;

    public RankingRepository(AppDbContext db) => _db = db;

    public Task<StageBestRecord?> GetBestRecordAsync(string userId, int stageId, CancellationToken ct)
        => _db.StageBestRecords.FirstOrDefaultAsync(r => r.UserId == userId && r.StageId == stageId, ct);

    public async Task UpsertBestRecordAsync(string userId, int stageId, long clearTimeMs, int score, CancellationToken ct)
    {
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $@"INSERT INTO stage_best_records (user_id, stage_id, best_clear_time_ms, best_score, cleared_at)
               VALUES ({userId}, {stageId}, {clearTimeMs}, {score}, NOW())
               ON CONFLICT (user_id, stage_id) DO UPDATE
                 SET best_clear_time_ms = {clearTimeMs},
                     best_score = {score},
                     cleared_at = NOW()
               WHERE stage_best_records.best_clear_time_ms > {clearTimeMs}", ct);
    }

    public async Task UpsertRankingCacheAsync(string userId, long totalScore, int stagesCleared, CancellationToken ct)
    {
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $@"INSERT INTO user_ranking_cache (user_id, total_score, stages_cleared, updated_at)
               VALUES ({userId}, {totalScore}, {stagesCleared}, NOW())
               ON CONFLICT (user_id) DO UPDATE
                 SET total_score = {totalScore}, stages_cleared = {stagesCleared}, updated_at = NOW()", ct);
    }

    public Task<List<StageBestRecord>> GetUserBestRecordsAsync(string userId, CancellationToken ct)
        => _db.StageBestRecords.Where(r => r.UserId == userId).ToListAsync(ct);

    public Task<List<StageBestRecord>> GetAllBestRecordsAsync(CancellationToken ct)
        => _db.StageBestRecords.ToListAsync(ct);

    public Task<List<UserRankingCache>> GetAllRankingCachesAsync(CancellationToken ct)
        => _db.RankingCaches.ToListAsync(ct);
}
