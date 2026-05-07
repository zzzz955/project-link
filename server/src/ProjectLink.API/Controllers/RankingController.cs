using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ProjectLink.Application.Ranking;

namespace ProjectLink.API.Controllers;

[ApiController]
[Route("api/ranking")]
[Authorize]
[EnableRateLimiting("ranking")]
public class RankingController : ControllerBase
{
    private readonly RankingService _ranking;

    public RankingController(RankingService ranking) => _ranking = ranking;

    [HttpGet("stage/{stageId:int}")]
    public async Task<IActionResult> GetStage(int stageId, [FromQuery] int top = 100, CancellationToken ct = default)
    {
        top = Math.Clamp(top, 1, 500);
        var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Ok(await _ranking.GetStageRankingAsync(stageId, top, callerId, ct));
    }

    [HttpGet("global/stages")]
    public async Task<IActionResult> GetGlobalStages([FromQuery] int top = 100, CancellationToken ct = default)
    {
        top = Math.Clamp(top, 1, 500);
        var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Ok(await _ranking.GetGlobalStagesRankingAsync(top, callerId, ct));
    }

    [HttpGet("global/score")]
    public async Task<IActionResult> GetGlobalScore([FromQuery] int top = 100, CancellationToken ct = default)
    {
        top = Math.Clamp(top, 1, 500);
        var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Ok(await _ranking.GetGlobalScoreRankingAsync(top, callerId, ct));
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMe(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        return Ok(await _ranking.GetMyRankAsync(userId, ct));
    }
}
