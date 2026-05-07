using ProjectLink.Domain.Entities;

namespace ProjectLink.Domain.Interfaces;

public interface IInventoryRepository
{
    Task<List<Inventory>> GetAllAsync(string userId, CancellationToken ct);
    Task<int> GrantAsync(string userId, int itemId, int quantity, CancellationToken ct);
    // Returns new quantity; throws InsufficientInventoryException if quantity < requested
    Task<int> DeductAsync(string userId, int itemId, int quantity, CancellationToken ct);
}
