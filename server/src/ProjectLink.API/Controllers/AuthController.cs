using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectLink.API;
using ProjectLink.Contracts.Account;

namespace ProjectLink.API.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    readonly ProjectLinkConfiguration _config;
    readonly IHttpClientFactory _httpFactory;
    readonly bool _useMock;

    public AuthController(ProjectLinkConfiguration config, IHttpClientFactory httpFactory)
    {
        _config   = config;
        _httpFactory = httpFactory;
        _useMock  = config.Auth.UseMock;
    }

    [HttpPost("guest")]
    public async Task<IActionResult> Guest(CancellationToken ct)
    {
        if (_useMock)
        {
            const string userId = "guest";
            return Ok(new AuthResponse
            {
                AccessToken  = $"{MockAuthenticationHandler.TokenPrefix}{userId}",
                RefreshToken = "mock-refresh",
                ExpiresAt    = DateTimeOffset.UtcNow.AddDays(30).ToString("O"),
                Profile = new AccountMeResponse
                {
                    UserId      = userId,
                    DisplayName = "Guest",
                    IsGuest     = true,
                    AvatarId    = 1,
                    CreatedAt   = DateTimeOffset.UtcNow.ToString("O"),
                },
            });
        }

        var authority = _config.Auth.JwtAuthority.TrimEnd('/');
        var http      = _httpFactory.CreateClient();
        var response  = await http.PostAsync($"{authority}/auth/guest", null, ct);
        var body      = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode, body);

        return Content(body, "application/json");
    }
}
