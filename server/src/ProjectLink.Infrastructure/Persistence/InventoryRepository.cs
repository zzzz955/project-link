using Microsoft.EntityFrameworkCore;
using ProjectLink.Domain.Entities;
using ProjectLink.Domain.Exceptions;
using ProjectLink.Domain.Interfaces;

namespace ProjectLink.Infrastructure.Persistence;

public class InventoryRepository : IInventoryRepository
{
    private readonly AppDbContext _db;

    public InventoryRepository(AppDbContext db) => _db = db;

    public Task<List<Inventory>> GetAllAsync(string userId, CancellationToken ct)
        => _db.Inventories.Where(i => i.UserId == userId && i.Quantity > 0).ToListAsync(ct);

    public async Task<int> GrantAsync(string userId, int itemId, int quantity, CancellationToken ct)
    {
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $@"INSERT INTO inventory (user_id, item_id, quantity)
               VALUES ({userId}, {itemId}, {quantity})
               ON DUPLICATE KEY UPDATE quantity = quantity + {quantity}", ct);

        _db.ChangeTracker.Clear();
        var row = await _db.Inventories.FirstAsync(i => i.UserId == userId && i.ItemId == itemId, ct);
        return row.Quantity;
    }

    public async Task<int> DeductAsync(string userId, int itemId, int quantity, CancellationToken ct)
    {
        var affected = await _db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE inventory SET quantity = quantity - {quantity} WHERE user_id = {userId} AND item_id = {itemId} AND quantity >= {quantity}", ct);

        if (affected == 0)
            throw new InsufficientInventoryException();

        _db.ChangeTracker.Clear();
        var row = await _db.Inventories.FirstAsync(i => i.UserId == userId && i.ItemId == itemId, ct);
        return row.Quantity;
    }
}
