using Microsoft.EntityFrameworkCore;
using ProjectLink.Domain.Entities;
using ProjectLink.Domain.Interfaces;

namespace ProjectLink.Infrastructure.Persistence;

public class DailyChallengeRepository : IDailyChallengeRepository
{
    private readonly AppDbContext _db;

    public DailyChallengeRepository(AppDbContext db) => _db = db;

    public Task<DailyChallengeProgress?> GetForDateAsync(string userId, DateOnly date, CancellationToken ct)
        => _db.DailyChallengeProgresses
               .FirstOrDefaultAsync(p => p.UserId == userId && p.ChallengeDate == date, ct);

    public async Task<int> IncrementPlayCountAsync(string userId, DateOnly date, CancellationToken ct)
    {
        await _db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO daily_challenge_progress (user_id, challenge_date, play_count, completed, streak_days, created_at)
            VALUES ({userId}, {date}, 1, false, 0, NOW())
            ON CONFLICT (user_id, challenge_date) DO UPDATE
              SET play_count = daily_challenge_progress.play_count + 1
            """, ct);

        _db.ChangeTracker.Clear();
        var row = await _db.DailyChallengeProgresses
            .FirstAsync(p => p.UserId == userId && p.ChallengeDate == date, ct);
        return row.PlayCount;
    }
}
