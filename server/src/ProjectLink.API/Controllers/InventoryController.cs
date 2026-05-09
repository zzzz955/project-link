using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectLink.Application.Inventory;
using ProjectLink.Application.Stage;
using ProjectLink.Contracts.Item;
using ProjectLink.Domain.Exceptions;
using ProjectLink.Domain.Interfaces;
using ProjectLink.Domain.Stage;

namespace ProjectLink.API.Controllers;

[ApiController]
[Authorize]
public class InventoryController : ControllerBase
{
    private readonly InventoryService  _inventory;
    private readonly StageService      _stage;
    private readonly IStageSessionCache _sessionCache;

    public InventoryController(InventoryService inventory, StageService stage, IStageSessionCache sessionCache)
    {
        _inventory    = inventory;
        _stage        = stage;
        _sessionCache = sessionCache;
    }

    [HttpGet("api/inventory")]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        return Ok(await _inventory.GetAsync(userId, ct));
    }

    [HttpPost("api/items/purchase")]
    public async Task<IActionResult> Purchase([FromBody] ItemPurchaseRequest req, CancellationToken ct)
    {
        var userId        = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var correlationId = HttpContext.Items["CorrelationId"] as string ?? HttpContext.TraceIdentifier;
        return Ok(await _inventory.PurchaseAsync(userId, req.ItemId, req.Quantity, correlationId, ct));
    }

    [HttpPost("api/items/use")]
    public async Task<IActionResult> Use([FromBody] ItemUseRequest req, CancellationToken ct)
    {
        var userId  = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var session = await _sessionCache.GetAsync(userId, ct)
            ?? throw new StageSessionNotFoundException();

        if (session.Token != req.StageSessionToken)
            throw new StageSessionNotFoundException();

        if (!session.IsSetupPhase)
            throw new StageNotInSetupPhaseException();

        var result = await _inventory.UseAsync(userId, req.Items, ct);

        // Record items used in the stage session
        await _stage.RecordItemUseAsync(userId, session.StageId, req.StageSessionToken,
            req.Items.Select(i => new ItemUsedEntry { ItemId = i.ItemId, Quantity = i.Quantity }).ToList(), ct);

        return Ok(result);
    }
}
