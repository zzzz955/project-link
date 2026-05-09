using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectLink.Contracts.Account;

namespace ProjectLink.API.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    [HttpPost("guest")]
    public IActionResult Guest()
    {
        const string userId = "guest";

        return Ok(new AuthResponse
        {
            AccessToken = $"{MockAuthenticationHandler.TokenPrefix}{userId}",
            RefreshToken = "mock-refresh",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30).ToString("O"),
            Profile = new AccountMeResponse
            {
                UserId = userId,
                DisplayName = "Guest",
                IsGuest = true,
                AvatarId = 1,
                CreatedAt = DateTimeOffset.UtcNow.ToString("O"),
            },
        });
    }
}
