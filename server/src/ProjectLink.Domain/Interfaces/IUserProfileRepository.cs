using ProjectLink.Domain.Entities;

namespace ProjectLink.Domain.Interfaces;

public interface IUserProfileRepository
{
    Task<UserProfile?> GetByIdAsync(string userId, CancellationToken ct);

    // INSERT ... ON CONFLICT DO UPDATE SET last_login_at = NOW()
    // Sets account_created_at = NOW() only on first insert.
    Task UpsertAsync(string userId, string displayName, CancellationToken ct);
}
