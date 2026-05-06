using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectLink.Application.Progress;
using ProjectLink.Domain.Entities;

namespace ProjectLink.API.Controllers;

[ApiController]
[Route("api/progress")]
public class ProgressController : ControllerBase
{
    private readonly GetProgressQueryHandler    _getHandler;
    private readonly UpsertProgressCommandHandler _upsertHandler;

    public ProgressController(GetProgressQueryHandler getHandler, UpsertProgressCommandHandler upsertHandler)
    {
        _getHandler    = getHandler;
        _upsertHandler = upsertHandler;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _getHandler.HandleAsync(new GetProgressQuery(userId, ct));
        return Ok(result);
    }

    [HttpPost("batch")]
    [Authorize]
    public async Task<IActionResult> UpsertBatch([FromBody] IEnumerable<StageProgress> records, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await _upsertHandler.HandleAsync(new UpsertProgressCommand(userId, records, ct));
        return NoContent();
    }
}
