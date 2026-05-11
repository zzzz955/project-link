using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.IdentityModel.Tokens;
using ProjectLink.Contracts.Account;
using ProjectLink.Contracts.Lobby;
using Xunit;

namespace ProjectLink.API.Tests;

public sealed class AuthIntegrationTests : IClassFixture<ApiTestFactory>
{
    private readonly ApiTestFactory _factory;

    public AuthIntegrationTests(ApiTestFactory factory) => _factory = factory;

    [Fact]
    public async Task ValidPlatformJwt_CanFetchLobby()
    {
        using var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            _factory.CreatePlatformToken());

        var response = await client.GetAsync("/api/lobby");

        Assert.True(response.StatusCode == HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var lobby = await response.Content.ReadFromJsonAsync<LobbyStateResponse>();
        Assert.NotNull(lobby);
        Assert.Equal("Platform User", lobby.Profile.DisplayName);
        Assert.Equal(1200, lobby.Currency.SoftAmount);
        Assert.Equal(2, lobby.ProgressSummary.NextUnlockedStageId);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("expired")]
    public async Task InvalidOrExpiredJwt_ReturnsUnauthorized(string tokenKind)
    {
        using var client = CreateClient();
        var token = tokenKind == "expired"
            ? _factory.CreatePlatformToken(expiresUtc: DateTime.UtcNow.AddMinutes(-1))
            : _factory.CreatePlatformToken(signingKey: new SymmetricSecurityKey(Encoding.UTF8.GetBytes("wrong-projectlink-api-test-key-32b")));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/lobby");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PlatformGuestLoginToken_CanFetchLobby()
    {
        using var client = CreateClient();
        var login = await client.PostAsync("/api/auth/guest", null);
        login.EnsureSuccessStatusCode();

        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var lobby = await client.GetAsync("/api/lobby");

        Assert.True(lobby.StatusCode == HttpStatusCode.OK, await lobby.Content.ReadAsStringAsync());
    }

    private HttpClient CreateClient()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
        });
        client.DefaultRequestHeaders.Add("X-Client-Version", ApiTestFactory.ClientVersion);
        client.DefaultRequestHeaders.Add("X-Protocol-Version", ApiTestFactory.ProtocolVersion);
        return client;
    }
}
