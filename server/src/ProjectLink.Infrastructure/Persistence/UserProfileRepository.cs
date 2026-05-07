using Microsoft.EntityFrameworkCore;
using ProjectLink.Domain.Entities;
using ProjectLink.Domain.Interfaces;
using StackExchange.Redis;

namespace ProjectLink.Infrastructure.Persistence;

public class UserProfileRepository : IUserProfileRepository
{
    private readonly AppDbContext _db;
    private readonly IDatabase    _redis;

    private static readonly TimeSpan ProfileExistsTtl = TimeSpan.FromHours(24);

    public UserProfileRepository(AppDbContext db, IConnectionMultiplexer redis)
    {
        _db    = db;
        _redis = redis.GetDatabase();
    }

    public Task<UserProfile?> GetByIdAsync(string userId, CancellationToken ct)
        => _db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId, ct);

    public async Task UpsertAsync(string userId, string displayName, CancellationToken ct)
    {
        // Skip DB hit if profile already exists (Redis flag set on first insert)
        var cacheKey = $"user_profile_exists:{userId}";
        if (await _redis.KeyExistsAsync(cacheKey)) return;

        await _db.Database.ExecuteSqlInterpolatedAsync(
            $@"INSERT INTO user_profiles (user_id, display_name, account_created_at, last_login_at)
               VALUES ({userId}, {displayName}, NOW(), NOW())
               ON CONFLICT (user_id) DO UPDATE SET last_login_at = NOW()",
            ct);

        await _redis.StringSetAsync(cacheKey, "1", ProfileExistsTtl);
    }
}
