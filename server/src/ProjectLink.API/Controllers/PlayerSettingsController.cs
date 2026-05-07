using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectLink.Application.Settings;
using ProjectLink.Contracts.Settings;

namespace ProjectLink.API.Controllers;

[ApiController]
[Route("api/settings")]
[Authorize]
public class PlayerSettingsController : ControllerBase
{
    private readonly PlayerSettingsService _service;

    public PlayerSettingsController(PlayerSettingsService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        return Ok(await _service.GetAsync(userId, ct));
    }

    [HttpPatch]
    public async Task<IActionResult> Update([FromBody] PlayerSettingsUpdateRequest req, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        return Ok(await _service.UpdateAsync(userId, req, ct));
    }
}
