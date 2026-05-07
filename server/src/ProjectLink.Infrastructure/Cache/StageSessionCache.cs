using System.Text.Json;
using ProjectLink.Domain.Interfaces;
using ProjectLink.Domain.Stage;
using StackExchange.Redis;

namespace ProjectLink.Infrastructure.Cache;

public class StageSessionCache : IStageSessionCache
{
    private readonly IDatabase _redis;

    public StageSessionCache(IConnectionMultiplexer redis) => _redis = redis.GetDatabase();

    private static string Key(string userId) => $"stage_session:{userId}";

    public async Task<StageSession?> GetAsync(string userId, CancellationToken ct)
    {
        var value = await _redis.StringGetAsync(Key(userId));
        return value.HasValue ? JsonSerializer.Deserialize<StageSession>(value!) : null;
    }

    public Task SetAsync(string userId, StageSession session, TimeSpan ttl, CancellationToken ct)
        => _redis.StringSetAsync(Key(userId), JsonSerializer.Serialize(session), ttl);

    public Task DeleteAsync(string userId, CancellationToken ct)
        => _redis.KeyDeleteAsync(Key(userId));
}
