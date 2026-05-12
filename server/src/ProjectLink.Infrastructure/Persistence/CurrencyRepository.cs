using Microsoft.EntityFrameworkCore;
using ProjectLink.Domain.Entities;
using ProjectLink.Domain.Exceptions;
using ProjectLink.Domain.Interfaces;

namespace ProjectLink.Infrastructure.Persistence;

public class CurrencyRepository : ICurrencyRepository
{
    private readonly AppDbContext _db;

    public CurrencyRepository(AppDbContext db) => _db = db;

    public async Task<long> GetBalanceAsync(string userId, CancellationToken ct)
    {
        var row = await _db.UserCurrencies.FirstOrDefaultAsync(c => c.UserId == userId, ct);
        return row?.SoftAmount ?? 0;
    }

    public async Task<long> GrantAsync(string userId, long amount, string reason, string transactionId, string correlationId, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT IGNORE INTO user_currency (user_id, soft_amount) VALUES ({userId}, 0)", ct);

        var row = await _db.UserCurrencies
            .FromSqlInterpolated($"SELECT * FROM user_currency WHERE user_id = {userId} FOR UPDATE")
            .FirstAsync(ct);

        var balanceBefore = row.SoftAmount;
        row.SoftAmount += amount;

        _db.CurrencyLogs.Add(new CurrencyLog
        {
            UserId        = userId,
            TransactionId = transactionId,
            CurrencyType  = "soft",
            Delta         = amount,
            BalanceBefore = balanceBefore,
            BalanceAfter  = row.SoftAmount,
            Reason        = reason,
            CorrelationId = correlationId,
            CreatedAt     = DateTimeOffset.UtcNow
        });

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return row.SoftAmount;
    }

    public async Task<long> DeductAsync(string userId, long amount, string reason, string transactionId, string correlationId, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT IGNORE INTO user_currency (user_id, soft_amount) VALUES ({userId}, 0)", ct);

        var row = await _db.UserCurrencies
            .FromSqlInterpolated($"SELECT * FROM user_currency WHERE user_id = {userId} FOR UPDATE")
            .FirstAsync(ct);

        if (row.SoftAmount < amount)
            throw new InsufficientFundsException();

        var balanceBefore = row.SoftAmount;
        row.SoftAmount -= amount;

        _db.CurrencyLogs.Add(new CurrencyLog
        {
            UserId        = userId,
            TransactionId = transactionId,
            CurrencyType  = "soft",
            Delta         = -amount,
            BalanceBefore = balanceBefore,
            BalanceAfter  = row.SoftAmount,
            Reason        = reason,
            CorrelationId = correlationId,
            CreatedAt     = DateTimeOffset.UtcNow
        });

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return row.SoftAmount;
    }
}
