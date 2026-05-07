using ProjectLink.Domain.Stage;

namespace ProjectLink.Domain.Interfaces;

public interface IStageSessionCache
{
    Task<StageSession?> GetAsync(string userId, CancellationToken ct);
    Task SetAsync(string userId, StageSession session, TimeSpan ttl, CancellationToken ct);
    Task DeleteAsync(string userId, CancellationToken ct);
}
