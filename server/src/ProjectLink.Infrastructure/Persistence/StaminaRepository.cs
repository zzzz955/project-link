using Microsoft.EntityFrameworkCore;
using ProjectLink.Domain.Entities;
using ProjectLink.Domain.Exceptions;
using ProjectLink.Domain.Interfaces;

namespace ProjectLink.Infrastructure.Persistence;

public class StaminaRepository : IStaminaRepository
{
    private readonly AppDbContext _db;

    public StaminaRepository(AppDbContext db) => _db = db;

    private static void ApplyRecharge(StaminaState state, int maxStamina, int rechargeIntervalMinutes)
    {
        if (state.Current >= maxStamina) return;
        var ticks = (int)((DateTimeOffset.UtcNow - state.LastRechargedAt).TotalMinutes / rechargeIntervalMinutes);
        if (ticks <= 0) return;
        state.Current         = Math.Min(maxStamina, state.Current + ticks);
        state.LastRechargedAt += TimeSpan.FromMinutes(ticks * rechargeIntervalMinutes);
    }

    private async Task<StaminaState> EnsureAndLockAsync(string userId, int maxStamina, CancellationToken ct)
    {
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT IGNORE INTO stamina_state (user_id, current, last_recharged_at) VALUES ({userId}, {maxStamina}, NOW())", ct);

        return await _db.StaminaStates
            .FromSqlInterpolated($"SELECT * FROM stamina_state WHERE user_id = {userId} FOR UPDATE")
            .FirstAsync(ct);
    }

    public async Task<StaminaState> GetComputedAsync(string userId, int maxStamina, int rechargeIntervalMinutes, CancellationToken ct)
    {
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT IGNORE INTO stamina_state (user_id, current, last_recharged_at) VALUES ({userId}, {maxStamina}, NOW())", ct);

        var state = await _db.StaminaStates.FirstAsync(s => s.UserId == userId, ct);
        ApplyRecharge(state, maxStamina, rechargeIntervalMinutes);
        // Persist lazily computed recharge without a lock (best-effort read path)
        await _db.SaveChangesAsync(ct);
        return state;
    }

    public async Task<StaminaState> DeductAsync(string userId, int maxStamina, int rechargeIntervalMinutes, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        var state = await EnsureAndLockAsync(userId, maxStamina, ct);
        ApplyRecharge(state, maxStamina, rechargeIntervalMinutes);

        if (state.Current <= 0)
            throw new InsufficientStaminaException();

        state.Current -= 1;

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return state;
    }

    public async Task<(StaminaState state, int added)> AddAsync(string userId, int maxStamina, int rechargeIntervalMinutes, int toAdd, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        var state  = await EnsureAndLockAsync(userId, maxStamina, ct);
        ApplyRecharge(state, maxStamina, rechargeIntervalMinutes);

        var before        = state.Current;
        state.Current     = Math.Min(maxStamina, state.Current + toAdd);
        var added         = state.Current - before;

        if (before >= maxStamina)
            state.LastRechargedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return (state, added);
    }
}
