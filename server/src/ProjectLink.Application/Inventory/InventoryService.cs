using ProjectLink.Contracts.Item;
using ProjectLink.Domain.Interfaces;

namespace ProjectLink.Application.Inventory;

public class InventoryService
{
    private readonly IInventoryRepository _repo;
    private readonly ICurrencyRepository  _currency;
    private readonly IStaticDataService   _staticData;

    public InventoryService(IInventoryRepository repo, ICurrencyRepository currency, IStaticDataService staticData)
    {
        _repo       = repo;
        _currency   = currency;
        _staticData = staticData;
    }

    public async Task<InventoryResponse> GetAsync(string userId, CancellationToken ct)
    {
        var slots = await _repo.GetAllAsync(userId, ct);
        return new InventoryResponse
        {
            Items = slots.Select(s => new InventorySlot { ItemId = s.ItemId, Quantity = s.Quantity }).ToList()
        };
    }

    public async Task<ItemPurchaseResponse> PurchaseAsync(string userId, int itemId, int quantity, string correlationId, CancellationToken ct)
    {
        var item = _staticData.GetItem(itemId)
            ?? throw new Domain.Exceptions.InvalidStageResultException();

        var totalCost    = (long)item.CostSoft * quantity;
        var balanceAfter = await _currency.DeductAsync(userId, totalCost, $"item_purchase:{itemId}", Guid.NewGuid().ToString(), correlationId, ct);
        var qtyAfter     = await _repo.GrantAsync(userId, itemId, quantity, ct);

        return new ItemPurchaseResponse
        {
            ItemId           = itemId,
            QuantityAfter    = qtyAfter,
            SoftBalanceAfter = balanceAfter
        };
    }

    public async Task<ItemUseResponse> UseAsync(string userId, List<ItemUseEntry> items, CancellationToken ct)
    {
        var updatedSlots = new List<InventorySlot>();
        foreach (var entry in items)
        {
            var qtyAfter = await _repo.DeductAsync(userId, entry.ItemId, entry.Quantity, ct);
            updatedSlots.Add(new InventorySlot { ItemId = entry.ItemId, Quantity = qtyAfter });
        }
        return new ItemUseResponse { UpdatedSlots = updatedSlots };
    }
}
