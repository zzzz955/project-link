namespace ProjectLink.Contracts.Shop;

public class ShopPurchaseRequest
{
    public int     ProductId      { get; set; }
    public int     Quantity        { get; set; } = 1;
    public string? IapReceiptData  { get; set; }  // null for soft-currency purchases
}
