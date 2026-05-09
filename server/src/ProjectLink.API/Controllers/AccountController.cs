using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectLink.Application.UserProfile;
using ProjectLink.Contracts.Account;

namespace ProjectLink.API.Controllers;

[ApiController]
[Route("api/account")]
[Authorize]
public class AccountController : ControllerBase
{
    private readonly UserProfileService _profiles;

    public AccountController(UserProfileService profiles) => _profiles = profiles;

    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var profile = await _profiles.GetProfileAsync(userId, ct);

        return Ok(new AccountMeResponse
        {
            UserId = userId,
            DisplayName = profile?.DisplayName ?? User.FindFirstValue("name") ?? "",
            IsGuest = true,
            AvatarId = profile?.AvatarId ?? 1,
            CreatedAt = profile?.AccountCreatedAt.ToString("O") ?? "",
        });
    }
}
