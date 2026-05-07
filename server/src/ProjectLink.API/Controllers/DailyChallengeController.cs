using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectLink.Application.DailyChallenge;

namespace ProjectLink.API.Controllers;

[ApiController]
[Route("api/daily-challenge")]
[Authorize]
public class DailyChallengeController : ControllerBase
{
    private readonly DailyChallengeService _service;

    public DailyChallengeController(DailyChallengeService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        return Ok(await _service.GetAsync(userId, ct));
    }

    [HttpPost("complete")]
    public async Task<IActionResult> Complete(CancellationToken ct)
    {
        var userId        = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var correlationId = HttpContext.Items["CorrelationId"] as string ?? HttpContext.TraceIdentifier;
        return Ok(await _service.CompleteAsync(userId, correlationId, ct));
    }
}
