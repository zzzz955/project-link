namespace ProjectLink.Contracts.Item;

public class ItemPurchaseRequest
{
    public int ItemId { get; set; }
    public int Quantity { get; set; }
}

public class ItemUseEntry
{
    public int ItemId { get; set; }
    public int Quantity { get; set; }
}

public class ItemUseRequest
{
    public string StageSessionToken { get; set; } = "";
    public List<ItemUseEntry> Items { get; set; } = new List<ItemUseEntry>();
}
