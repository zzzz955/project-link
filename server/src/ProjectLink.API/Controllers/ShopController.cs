using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectLink.Application.Shop;
using ProjectLink.Contracts.Shop;

namespace ProjectLink.API.Controllers;

[ApiController]
[Route("api/shop")]
[Authorize]
public class ShopController : ControllerBase
{
    private readonly ShopService _shop;

    public ShopController(ShopService shop) => _shop = shop;

    [HttpGet("catalog")]
    public async Task<IActionResult> GetCatalog(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        return Ok(await _shop.GetCatalogAsync(userId, ct));
    }

    [HttpPost("purchase")]
    public async Task<IActionResult> Purchase([FromBody] ShopPurchaseRequest req, CancellationToken ct)
    {
        var userId        = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var correlationId = HttpContext.Items["CorrelationId"] as string ?? HttpContext.TraceIdentifier;
        return Ok(await _shop.PurchaseAsync(userId, req.ProductId, req.Quantity, correlationId, ct));
    }
}
