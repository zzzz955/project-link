using ProjectLink.Domain.Entities;

namespace ProjectLink.Domain.Interfaces;

public interface IProgressRepository
{
    Task<IEnumerable<StageProgress>> GetAllAsync(string userId, CancellationToken ct);
    Task UpsertBatchAsync(string userId, IEnumerable<StageProgress> records, CancellationToken ct);
    // Stage-end: keeps max stars (does not downgrade existing stars)
    Task UpsertClearAsync(string userId, int stageId, int stars, CancellationToken ct);
}
