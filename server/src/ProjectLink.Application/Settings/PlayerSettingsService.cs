using ProjectLink.Contracts.Settings;
using ProjectLink.Domain.Entities;
using ProjectLink.Domain.Interfaces;

namespace ProjectLink.Application.Settings;

public class PlayerSettingsService
{
    private readonly IPlayerSettingsRepository _repo;

    public PlayerSettingsService(IPlayerSettingsRepository repo) => _repo = repo;

    public async Task<PlayerSettingsResponse> GetAsync(string userId, CancellationToken ct)
    {
        var settings = await _repo.GetOrDefaultAsync(userId, ct);
        return ToResponse(settings);
    }

    public async Task<PlayerSettingsResponse> UpdateAsync(string userId, PlayerSettingsUpdateRequest req, CancellationToken ct)
    {
        var settings = await _repo.GetOrDefaultAsync(userId, ct);
        settings.UserId = userId;

        if (req.BgmEnabled.HasValue)           settings.BgmEnabled           = req.BgmEnabled.Value;
        if (req.SfxEnabled.HasValue)           settings.SfxEnabled           = req.SfxEnabled.Value;
        if (req.HapticsEnabled.HasValue)       settings.HapticsEnabled       = req.HapticsEnabled.Value;
        if (req.NotificationsEnabled.HasValue) settings.NotificationsEnabled = req.NotificationsEnabled.Value;
        if (req.Language != null)              settings.Language             = req.Language;

        await _repo.UpsertAsync(settings, ct);
        return ToResponse(settings);
    }

    private static PlayerSettingsResponse ToResponse(PlayerSettings s) => new()
    {
        BgmEnabled           = s.BgmEnabled,
        SfxEnabled           = s.SfxEnabled,
        HapticsEnabled       = s.HapticsEnabled,
        NotificationsEnabled = s.NotificationsEnabled,
        Language             = s.Language,
    };
}
