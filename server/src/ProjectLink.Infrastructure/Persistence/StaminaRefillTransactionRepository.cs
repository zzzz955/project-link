using Microsoft.EntityFrameworkCore;
using ProjectLink.Domain.Entities;
using ProjectLink.Domain.Exceptions;
using ProjectLink.Domain.Interfaces;
using ProjectLink.Domain.Utilities;

namespace ProjectLink.Infrastructure.Persistence;

public class StaminaRefillTransactionRepository : IStaminaRefillTransaction
{
    private readonly AppDbContext _db;

    public StaminaRefillTransactionRepository(AppDbContext db) => _db = db;

    public async Task<StaminaRefillDbResult> ExecuteAsync(
        string userId,
        int    maxStamina,
        int    rechargeSeconds,
        int    refillCostSoft,
        string correlationId,
        CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        // Ensure rows exist
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT IGNORE INTO stamina_state (user_id, current, last_recharged_at) VALUES ({userId}, {maxStamina}, NOW())", ct);
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT IGNORE INTO user_currency (user_id, soft_amount) VALUES ({userId}, 0)", ct);

        // Acquire locks
        var state = await _db.StaminaStates
            .FromSqlInterpolated($"SELECT * FROM stamina_state WHERE user_id = {userId} FOR UPDATE")
            .FirstAsync(ct);
        var currency = await _db.UserCurrencies
            .FromSqlInterpolated($"SELECT * FROM user_currency WHERE user_id = {userId} FOR UPDATE")
            .FirstAsync(ct);

        // Apply lazy recharge before checking fullness
        var elapsed = (int)((DateTimeOffset.UtcNow - state.LastRechargedAt).TotalSeconds / rechargeSeconds);
        if (elapsed > 0 && state.Current < maxStamina)
        {
            state.Current         = Math.Min(maxStamina, state.Current + elapsed);
            state.LastRechargedAt = state.LastRechargedAt.AddSeconds((long)elapsed * rechargeSeconds);
        }

        if (state.Current >= maxStamina)
            throw new StaminaAlreadyFullException();

        if (currency.SoftAmount < refillCostSoft)
            throw new InsufficientFundsException();

        var added         = maxStamina - state.Current;
        var balanceBefore = currency.SoftAmount;
        var now           = DateTimeOffset.UtcNow;

        state.Current         = maxStamina;
        state.LastRechargedAt = now;

        currency.SoftAmount -= refillCostSoft;

        _db.CurrencyLogs.Add(new CurrencyLog
        {
            UserId        = userId,
            TransactionId = IdHelper.NewId(),
            CurrencyType  = "soft",
            Delta         = -refillCostSoft,
            BalanceBefore = balanceBefore,
            BalanceAfter  = currency.SoftAmount,
            Reason        = "stamina_refill",
            CorrelationId = correlationId,
            CreatedAt     = now,
        });

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return new StaminaRefillDbResult
        {
            CurrentAfter     = maxStamina,
            Added            = added,
            SoftBalanceAfter = currency.SoftAmount,
            NextRechargeAt   = now.AddSeconds(rechargeSeconds),
        };
    }
}
