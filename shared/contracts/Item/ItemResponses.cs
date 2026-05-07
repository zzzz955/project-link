namespace ProjectLink.Contracts.Item;

public class InventorySlot
{
    public int ItemId { get; set; }
    public int Quantity { get; set; }
}

public class InventoryResponse
{
    public List<InventorySlot> Items { get; set; } = new List<InventorySlot>();
}

public class ItemPurchaseResponse
{
    public int ItemId { get; set; }
    public int QuantityAfter { get; set; }
    public long SoftBalanceAfter { get; set; }
}

public class ItemUseResponse
{
    public List<InventorySlot> UpdatedSlots { get; set; } = new List<InventorySlot>();
}
