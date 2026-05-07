using Microsoft.EntityFrameworkCore;
using ProjectLink.Domain.Entities;
using ProjectLink.Domain.Exceptions;
using ProjectLink.Domain.Interfaces;

namespace ProjectLink.Infrastructure.Persistence;

public class ShopPurchaseTransactionRepository : IShopPurchaseTransaction
{
    private readonly AppDbContext _db;

    public ShopPurchaseTransactionRepository(AppDbContext db) => _db = db;

    public async Task<ShopPurchaseDbResult> ExecuteAsync(
        string userId, int itemId, int quantity, long totalCost, string correlationId, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO user_currency (user_id, soft_amount) VALUES ({userId}, 0) ON CONFLICT DO NOTHING", ct);

        var currency = await _db.UserCurrencies
            .FromSqlInterpolated($"SELECT * FROM user_currency WHERE user_id = {userId} FOR UPDATE")
            .FirstAsync(ct);

        if (currency.SoftAmount < totalCost)
            throw new InsufficientFundsException();

        var balanceBefore = currency.SoftAmount;
        currency.SoftAmount -= totalCost;

        _db.CurrencyLogs.Add(new CurrencyLog
        {
            UserId        = userId,
            TransactionId = Guid.NewGuid().ToString(),
            CurrencyType  = "soft",
            Delta         = -totalCost,
            BalanceBefore = balanceBefore,
            BalanceAfter  = currency.SoftAmount,
            Reason        = $"shop_purchase:item:{itemId}",
            CorrelationId = correlationId,
            CreatedAt     = DateTimeOffset.UtcNow,
        });

        await _db.SaveChangesAsync(ct);

        await _db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO inventory (user_id, item_id, quantity)
            VALUES ({userId}, {itemId}, {quantity})
            ON CONFLICT (user_id, item_id) DO UPDATE
              SET quantity = inventory.quantity + {quantity}
            """, ct);

        _db.ChangeTracker.Clear();
        var inv = await _db.Inventories.FirstAsync(i => i.UserId == userId && i.ItemId == itemId, ct);

        await tx.CommitAsync(ct);

        return new ShopPurchaseDbResult
        {
            SoftBalanceAfter = currency.SoftAmount,
            QuantityAfter    = inv.Quantity,
        };
    }
}
