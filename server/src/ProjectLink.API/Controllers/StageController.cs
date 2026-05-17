using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ProjectLink.Application.Stage;
using ProjectLink.Contracts.Stage;

namespace ProjectLink.API.Controllers;

[ApiController]
[Route("api/stage/{stageId:int}")]
[Authorize]
[EnableRateLimiting("stage_start")]
public class StageController : ControllerBase
{
    private readonly StageService _stage;

    public StageController(StageService stage) => _stage = stage;

    [HttpPost("start")]
    public async Task<IActionResult> Start(int stageId, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        return Ok(await _stage.StartAsync(userId, stageId, ct));
    }

    [HttpPost("lock")]
    public async Task<IActionResult> Lock(int stageId, [FromBody] StageEndRequest req, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await _stage.LockAsync(userId, stageId, req.SessionToken, ct);
        return NoContent();
    }

    [HttpPost("end")]
    public async Task<IActionResult> End(int stageId, [FromBody] StageEndRequest req, CancellationToken ct)
    {
        var userId        = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var correlationId = HttpContext.Items["CorrelationId"] as string ?? HttpContext.TraceIdentifier;
        return Ok(await _stage.EndAsync(
            userId, stageId, req.SessionToken, req.Result, req.ClientElapsedMs, req.MovesUsed, correlationId, ct));
    }

    [HttpPost("extend")]
    public async Task<IActionResult> Extend(int stageId, [FromBody] StageEndRequest req, CancellationToken ct)
    {
        var userId        = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var correlationId = HttpContext.Items["CorrelationId"] as string ?? HttpContext.TraceIdentifier;
        return Ok(await _stage.ExtendAsync(userId, stageId, req.SessionToken, correlationId, ct));
    }
}
