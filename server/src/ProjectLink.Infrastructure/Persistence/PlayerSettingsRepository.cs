using Microsoft.EntityFrameworkCore;
using ProjectLink.Domain.Entities;
using ProjectLink.Domain.Interfaces;

namespace ProjectLink.Infrastructure.Persistence;

public class PlayerSettingsRepository : IPlayerSettingsRepository
{
    private readonly AppDbContext _db;

    public PlayerSettingsRepository(AppDbContext db) => _db = db;

    public async Task<PlayerSettings> GetOrDefaultAsync(string userId, CancellationToken ct)
    {
        var row = await _db.PlayerSettings.FirstOrDefaultAsync(s => s.UserId == userId, ct);
        return row ?? new PlayerSettings { UserId = userId, UpdatedAt = DateTimeOffset.UtcNow };
    }

    public async Task UpsertAsync(PlayerSettings settings, CancellationToken ct)
    {
        settings.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO player_settings
              (user_id, bgm_enabled, sfx_enabled, haptics_enabled, notifications_enabled, language, updated_at)
            VALUES
              ({settings.UserId}, {settings.BgmEnabled}, {settings.SfxEnabled},
               {settings.HapticsEnabled}, {settings.NotificationsEnabled}, {settings.Language}, {settings.UpdatedAt})
            ON CONFLICT (user_id) DO UPDATE SET
              bgm_enabled           = EXCLUDED.bgm_enabled,
              sfx_enabled           = EXCLUDED.sfx_enabled,
              haptics_enabled       = EXCLUDED.haptics_enabled,
              notifications_enabled = EXCLUDED.notifications_enabled,
              language              = EXCLUDED.language,
              updated_at            = EXCLUDED.updated_at
            """, ct);
    }
}
