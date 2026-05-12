using System.Text;
using System.Text.Json;
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
    static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    readonly ProjectLinkConfiguration _config;
    readonly IHttpClientFactory _httpFactory;
    readonly bool _useMock;

    public AuthController(ProjectLinkConfiguration config, IHttpClientFactory httpFactory)
    {
        _config      = config;
        _httpFactory = httpFactory;
        _useMock     = config.Auth.UseMock;
    }

    [HttpPost("guest")]
    public Task<IActionResult> Guest(CancellationToken ct)
        => _useMock ? Task.FromResult(MockAuthResponse()) : ProxyAndTransform("auth/guest", ct);

    [HttpPost("refresh")]
    public Task<IActionResult> Refresh(CancellationToken ct)
        => _useMock ? Task.FromResult(MockAuthResponse()) : ProxyAndTransform("auth/refresh", ct);

    [HttpPost("google")]
    public Task<IActionResult> Google(CancellationToken ct)
        => _useMock
            ? Task.FromResult<IActionResult>(BadRequest(new { errorCode = "PROVIDER_UNAVAILABLE" }))
            : ProxyAndTransform("auth/google", ct);

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        if (_useMock)
            return NoContent();

        var authority = _config.Auth.JwtAuthority.TrimEnd('/');
        var http      = _httpFactory.CreateClient();
        using var reader  = new StreamReader(Request.Body, leaveOpen: true);
        var requestBody   = await reader.ReadToEndAsync();
        using var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
        var response      = await http.PostAsync($"{authority}/auth/logout", content, ct);
        return response.IsSuccessStatusCode ? NoContent() : StatusCode((int)response.StatusCode);
    }

    IActionResult MockAuthResponse()
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

    async Task<IActionResult> ProxyAndTransform(string platformPath, CancellationToken ct)
    {
        var authority = _config.Auth.JwtAuthority.TrimEnd('/');
        var http      = _httpFactory.CreateClient();
        using var reader      = new StreamReader(Request.Body, leaveOpen: true);
        var requestBody       = await reader.ReadToEndAsync();
        using var content     = new StringContent(requestBody, Encoding.UTF8, "application/json");
        var response          = await http.PostAsync($"{authority}/{platformPath}", content, ct);
        var responseBody      = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode, responseBody);

        try
        {
            var session = JsonSerializer.Deserialize<PlatformSession>(responseBody, JsonOpts);
            return Ok(new AuthResponse
            {
                AccessToken  = session?.Tokens?.AccessToken             ?? "",
                RefreshToken = session?.Tokens?.RefreshToken            ?? "",
                ExpiresAt    = session?.Tokens?.AccessTokenExpiresAt    ?? "",
                Profile = new AccountMeResponse
                {
                    UserId  = session?.AccountId  ?? "",
                    IsGuest = session?.AccountType == "guest",
                },
            });
        }
        catch
        {
            return StatusCode(500, responseBody);
        }
    }

    sealed class PlatformSession
    {
        public string AccountId   { get; set; }
        public string AccountType { get; set; }
        public PlatformTokens Tokens { get; set; }
    }

    sealed class PlatformTokens
    {
        public string AccessToken          { get; set; }
        public string RefreshToken         { get; set; }
        public string AccessTokenExpiresAt { get; set; }
    }
}
