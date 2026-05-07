using ProjectLink.Domain.Interfaces;
using UserProfileEntity = ProjectLink.Domain.Entities.UserProfile;

namespace ProjectLink.Application.UserProfile;

public class UserProfileService
{
    private readonly IUserProfileRepository _repo;

    public UserProfileService(IUserProfileRepository repo) => _repo = repo;

    public Task EnsureProfileAsync(string userId, string displayName, CancellationToken ct)
        => _repo.UpsertAsync(userId, displayName, ct);

    public Task<UserProfileEntity?> GetProfileAsync(string userId, CancellationToken ct)
        => _repo.GetByIdAsync(userId, ct);
}
