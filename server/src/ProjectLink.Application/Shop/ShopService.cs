using ProjectLink.Contracts.Shop;
using ProjectLink.Domain.Exceptions;
using ProjectLink.Domain.Interfaces;

namespace ProjectLink.Application.Shop;

public class ShopService
{
    private readonly IStaticDataService      _staticData;
    private readonly ICurrencyRepository     _currency;
    private readonly IShopPurchaseTransaction _purchaseTx;

    public ShopService(
        IStaticDataService       staticData,
        ICurrencyRepository      currency,
        IShopPurchaseTransaction purchaseTx)
    {
        _staticData = staticData;
        _currency   = currency;
        _purchaseTx = purchaseTx;
    }

    public async Task<ShopCatalogResponse> GetCatalogAsync(string userId, CancellationToken ct)
    {
        var products = _staticData.GetShopCatalog()
            .Where(p => p.IsEnabled)
            .OrderBy(p => p.SortOrder)
            .Select(p => new ShopProductEntry
            {
                ProductId     = p.ProductId,
                Category      = p.Category,
                Name          = p.Name,
                GrantItemId   = p.GrantItemId,
                GrantQuantity = p.GrantQuantity,
                PriceSoft     = p.PriceSoft,
                PriceIapSku   = p.PriceIapSku,
                SortOrder     = p.SortOrder,
            })
            .ToList();

        var balance = await _currency.GetBalanceAsync(userId, ct);

        return new ShopCatalogResponse
        {
            Products    = products,
            SoftBalance = balance,
        };
    }

    public async Task<ShopPurchaseResponse> PurchaseAsync(
        string userId, int productId, int quantity, string correlationId, CancellationToken ct)
    {
        var product = _staticData.GetShopProduct(productId)
            ?? throw new ShopItemNotFoundException();

        if (!product.IsEnabled)
            throw new ShopItemNotFoundException();

        // Only ITEM category supports soft-currency purchase at launch
        if (product.Category != "ITEM")
            throw new ShopItemNotFoundException();

        if (quantity <= 0)
            throw new ShopItemNotFoundException();

        var totalCost = (long)product.PriceSoft * quantity;

        var result = await _purchaseTx.ExecuteAsync(
            userId, product.GrantItemId, product.GrantQuantity * quantity, totalCost, correlationId, ct);

        return new ShopPurchaseResponse
        {
            ProductId        = productId,
            SoftBalanceAfter = result.SoftBalanceAfter,
            InventoryUpdates = new List<ShopPurchaseInventoryUpdate>
            {
                new() { ItemId = product.GrantItemId, QuantityAfter = result.QuantityAfter }
            },
        };
    }
}
