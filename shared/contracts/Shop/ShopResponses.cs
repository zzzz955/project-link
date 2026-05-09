#nullable enable

using System.Collections.Generic;
namespace ProjectLink.Contracts.Shop
{

public class ShopProductEntry
{
    public int     ProductId     { get; set; }
    public string  Category      { get; set; } = "";
    public string  Name          { get; set; } = "";
    public int     GrantItemId   { get; set; }
    public int     GrantQuantity { get; set; }
    public int     PriceSoft     { get; set; }
    public string? PriceIapSku   { get; set; }
    public int     SortOrder     { get; set; }
}

public class ShopCatalogResponse
{
    public List<ShopProductEntry> Products    { get; set; } = new();
    public long                   SoftBalance { get; set; }
}

public class ShopPurchaseInventoryUpdate
{
    public int ItemId        { get; set; }
    public int QuantityAfter { get; set; }
}

public class ShopPurchaseResponse
{
    public int                               ProductId        { get; set; }
    public long                              SoftBalanceAfter { get; set; }
    public List<ShopPurchaseInventoryUpdate> InventoryUpdates { get; set; } = new();
}
}
