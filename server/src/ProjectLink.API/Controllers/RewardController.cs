using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectLink.Application.Reward;
using ProjectLink.Contracts.Reward;

namespace ProjectLink.API.Controllers;

[ApiController]
[Route("api/rewards")]
[Authorize]
public class RewardController : ControllerBase
{
    private readonly RewardService _rewards;

    public RewardController(RewardService rewards) => _rewards = rewards;

    [HttpPost("claim")]
    public async Task<IActionResult> Claim([FromBody] RewardClaimRequest req, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var correlationId = HttpContext.Items["CorrelationId"] as string ?? HttpContext.TraceIdentifier;

        return Ok(await _rewards.ClaimAsync(
            userId,
            req.RewardSource,
            req.RewardToken,
            req.Multiplier,
            correlationId,
            ct));
    }
}
