using Microsoft.EntityFrameworkCore;
using ProjectLink.Domain.Entities;
using ProjectLink.Domain.Interfaces;

namespace ProjectLink.Infrastructure.Persistence;

public class ProgressRepository : IProgressRepository
{
    private readonly AppDbContext _db;

    public ProgressRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<StageProgress>> GetAllAsync(string userId, CancellationToken ct)
        => await _db.StageProgress
            .Where(p => p.UserId == userId)
            .ToListAsync(ct);

    public async Task UpsertBatchAsync(string userId, IEnumerable<StageProgress> records, CancellationToken ct)
    {
        foreach (var incoming in records)
        {
            var existing = await _db.StageProgress
                .FirstOrDefaultAsync(p => p.UserId == userId && p.StageId == incoming.StageId, ct);

            if (existing is null)
            {
                _db.StageProgress.Add(new StageProgress
                {
                    UserId    = userId,
                    StageId   = incoming.StageId,
                    Stars     = incoming.Stars,
                    ClearedAt = incoming.ClearedAt,
                });
            }
            else if (incoming.ClearedAt > existing.ClearedAt)
            {
                existing.Stars     = incoming.Stars;
                existing.ClearedAt = incoming.ClearedAt;
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    public Task UpsertClearAsync(string userId, int stageId, int stars, CancellationToken ct)
        => _db.Database.ExecuteSqlInterpolatedAsync(
            $@"INSERT INTO stage_progress (user_id, stage_id, stars, cleared_at)
               VALUES ({userId}, {stageId}, {stars}, NOW())
               ON CONFLICT (user_id, stage_id) DO UPDATE
                 SET stars = GREATEST(stage_progress.stars, {stars}), cleared_at = NOW()", ct);
}
