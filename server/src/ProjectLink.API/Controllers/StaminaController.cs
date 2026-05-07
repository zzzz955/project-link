using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectLink.Application.Stamina;
using ProjectLink.Contracts.Stamina;

namespace ProjectLink.API.Controllers;

[ApiController]
[Route("api/stamina")]
[Authorize]
public class StaminaController : ControllerBase
{
    private readonly StaminaService _stamina;

    public StaminaController(StaminaService stamina) => _stamina = stamina;

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        return Ok(await _stamina.GetAsync(userId, ct));
    }

    [HttpPost("ad-reward")]
    public async Task<IActionResult> AdReward([FromBody] StaminaAdRewardRequest req, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        return Ok(await _stamina.AdRewardAsync(userId, req.AdToken, ct));
    }

    [HttpPost("extend")]
    public async Task<IActionResult> Extend(CancellationToken ct)
    {
        var userId        = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var correlationId = HttpContext.Items["CorrelationId"] as string ?? HttpContext.TraceIdentifier;
        return Ok(await _stamina.ExtendAsync(userId, correlationId, ct));
    }
}
