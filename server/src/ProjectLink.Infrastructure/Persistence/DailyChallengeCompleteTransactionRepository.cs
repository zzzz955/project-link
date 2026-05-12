using Microsoft.EntityFrameworkCore;
using ProjectLink.Domain.Entities;
using ProjectLink.Domain.Exceptions;
using ProjectLink.Domain.Interfaces;
using ProjectLink.Domain.Utilities;

namespace ProjectLink.Infrastructure.Persistence;

public class DailyChallengeCompleteTransactionRepository : IDailyChallengeCompleteTransaction
{
    private readonly AppDbContext _db;

    public DailyChallengeCompleteTransactionRepository(AppDbContext db) => _db = db;

    public async Task<DailyChallengeCompleteDbResult> ExecuteAsync(
        string                          userId,
        DateOnly                        challengeDate,
        int                             currentStreak,
        List<DailyChallengeRewardInput> rewards,
        string                          correlationId,
        CancellationToken               ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        // Lock the challenge row — NOWAIT so concurrent completion requests fail fast
        var challengeRow = await _db.DailyChallengeProgresses
            .FromSqlInterpolated($"SELECT * FROM daily_challenge_progress WHERE user_id = {userId} AND challenge_date = {challengeDate} FOR UPDATE NOWAIT")
            .FirstOrDefaultAsync(ct);

        if (challengeRow is null)
            throw new DailyChallengeNotCompletableException();
        if (challengeRow.Completed)
            throw new DailyChallengeAlreadyCompletedException();

        var newStreak = currentStreak + 1;
        challengeRow.Completed      = true;
        challengeRow.StreakDays     = newStreak;
        challengeRow.LastStreakDate = challengeDate;

        await _db.SaveChangesAsync(ct);

        // Grant rewards
        long softBalanceAfter       = 0;
        var  inventoryAfter         = new Dictionary<int, int>();
        bool hasSoftReward          = rewards.Any(r => r.RewardType == "SOFT_CURRENCY");

        if (hasSoftReward)
        {
            await _db.Database.ExecuteSqlInterpolatedAsync(
                $"INSERT IGNORE INTO user_currency (user_id, soft_amount) VALUES ({userId}, 0)", ct);

            var currency = await _db.UserCurrencies
                .FromSqlInterpolated($"SELECT * FROM user_currency WHERE user_id = {userId} FOR UPDATE")
                .FirstAsync(ct);

            foreach (var reward in rewards.Where(r => r.RewardType == "SOFT_CURRENCY"))
            {
                var balanceBefore     = currency.SoftAmount;
                currency.SoftAmount  += reward.Amount;

                _db.CurrencyLogs.Add(new CurrencyLog
                {
                    UserId        = userId,
                    TransactionId = IdHelper.NewId(),
                    CurrencyType  = "soft",
                    Delta         = reward.Amount,
                    BalanceBefore = balanceBefore,
                    BalanceAfter  = currency.SoftAmount,
                    Reason        = "daily_challenge_complete",
                    CorrelationId = correlationId,
                    CreatedAt     = DateTimeOffset.UtcNow,
                });
            }

            await _db.SaveChangesAsync(ct);
            softBalanceAfter = currency.SoftAmount;
        }

        foreach (var reward in rewards.Where(r => r.RewardType == "ITEM"))
        {
            await _db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO inventory (user_id, item_id, quantity)
                VALUES ({userId}, {reward.RewardId}, {reward.Amount})
                ON DUPLICATE KEY UPDATE quantity = quantity + {reward.Amount}
                """, ct);

            _db.ChangeTracker.Clear();
            var inv = await _db.Inventories.FirstAsync(i => i.UserId == userId && i.ItemId == reward.RewardId, ct);
            inventoryAfter[reward.RewardId] = inv.Quantity;
        }

        if (!hasSoftReward)
        {
            var row = await _db.UserCurrencies.FirstOrDefaultAsync(c => c.UserId == userId, ct);
            softBalanceAfter = row?.SoftAmount ?? 0;
        }

        await tx.CommitAsync(ct);

        return new DailyChallengeCompleteDbResult
        {
            NewStreakDays    = newStreak,
            SoftBalanceAfter = softBalanceAfter,
            InventoryAfter   = inventoryAfter,
        };
    }
}
