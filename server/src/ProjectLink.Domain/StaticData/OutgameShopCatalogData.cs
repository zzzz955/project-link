namespace ProjectLink.Domain.StaticData;

public class OutgameShopCatalogData
{
    public int     ProductId     { get; set; }
    public string  Category      { get; set; } = "";  // "ITEM" | "COIN" | "BUNDLE" | "NO_ADS"
    public string  Name          { get; set; } = "";
    public int     GrantItemId   { get; set; }
    public int     GrantQuantity { get; set; }
    public int     PriceSoft     { get; set; }
    public string? PriceIapSku   { get; set; }
    public int     SortOrder     { get; set; }
    public bool    IsEnabled     { get; set; }
}
