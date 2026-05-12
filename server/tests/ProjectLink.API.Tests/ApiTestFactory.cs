using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using ProjectLink.Application.Session;
using ProjectLink.Contracts.Account;
using ProjectLink.Domain.Entities;
using ProjectLink.Domain.Interfaces;
using ProjectLink.Domain.StaticData;
using StackExchange.Redis;

namespace ProjectLink.API.Tests;

public sealed class ApiTestFactory : WebApplicationFactory<Program>
{
    public const string ClientVersion = "test-client";
    public const string ProtocolVersion = "test-protocol";
    public const string Audience = "projectlink-tests";
    public const string AppClientId = "projectlink";
    public const string SessionId = "session-1";

    private readonly Dictionary<string, string?> _previousEnv = new();
    private readonly SymmetricSecurityKey _signingKey =
        new(Encoding.UTF8.GetBytes("projectlink-api-test-signing-key-32b"));

    public ApiTestFactory()
    {
        SetEnv("GAME_ENV", "test");
        SetEnv("DB_HOST", "localhost");
        SetEnv("DB_PORT", "3306");
        SetEnv("DB_NAME", "projectlink_tests");
        SetEnv("DB_USER", "projectlink");
        SetEnv("DB_PASSWORD", "test-password");
        SetEnv("REDIS_HOST", "localhost");
        SetEnv("REDIS_PORT", "6379");
        SetEnv("AUTH_USE_MOCK", "false");
        SetEnv("JWT_AUTHORITY", "https://platform.test");
        SetEnv("JWT_AUDIENCE", Audience);
        SetEnv("APP_CLIENT_ID", AppClientId);
        SetEnv("APP_ALLOWED_CLIENT_VERSION", ClientVersion);
        SetEnv("APP_ALLOWED_PROTOCOL_VERSION", ProtocolVersion);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Auth:UseMock", "false");
        builder.UseSetting("Jwt:Authority", "https://platform.test");
        builder.UseSetting("Jwt:Audience", Audience);
        builder.UseSetting("App:ClientId", AppClientId);
        builder.UseSetting("App:AllowedClientVersion", ClientVersion);
        builder.UseSetting("App:AllowedProtocolVersion", ProtocolVersion);
        builder.UseSetting("Redis:Connection", "localhost:6379,abortConnect=false");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:UseMock"] = "false",
                ["Jwt:Authority"] = "https://platform.test",
                ["Jwt:Audience"] = Audience,
                ["App:ClientId"] = AppClientId,
                ["App:AllowedClientVersion"] = ClientVersion,
                ["App:AllowedProtocolVersion"] = ProtocolVersion,
                ["Redis:Connection"] = "localhost:6379,abortConnect=false",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IHostedService>();
            services.RemoveAll<IConnectionMultiplexer>();
            services.RemoveAll<IHttpClientFactory>();
            services.RemoveAll<IUserProfileRepository>();
            services.RemoveAll<ISessionRepository>();
            services.RemoveAll<ISessionCache>();
            services.RemoveAll<IStaminaRepository>();
            services.RemoveAll<ICurrencyRepository>();
            services.RemoveAll<IProgressRepository>();
            services.RemoveAll<IDailyChallengeRepository>();
            services.RemoveAll<IStaticDataService>();

            services.AddSingleton<IUserProfileRepository, InMemoryUserProfileRepository>();
            services.AddSingleton<ISessionRepository, TestSessionRepository>();
            services.AddSingleton<ISessionCache, TestSessionCache>();
            services.AddSingleton<IStaminaRepository, TestStaminaRepository>();
            services.AddSingleton<ICurrencyRepository, TestCurrencyRepository>();
            services.AddSingleton<IProgressRepository, TestProgressRepository>();
            services.AddSingleton<IDailyChallengeRepository, TestDailyChallengeRepository>();
            services.AddSingleton<IStaticDataService, TestStaticDataService>();
            services.AddSingleton<IHttpClientFactory>(new TestPlatformHttpClientFactory(() =>
                CreatePlatformToken("platform-guest", "Platform Guest")));

            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, opts =>
            {
                opts.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = _signingKey,
                    IssuerSigningKeyResolver = null,
                    ValidateIssuer = false,
                    ValidateAudience = true,
                    ValidAudience = Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                };
                opts.Events = new JwtBearerEvents
                {
                    OnTokenValidated = ctx =>
                    {
                        if (ctx.Principal?.FindFirst("app")?.Value != AppClientId)
                            ctx.Fail("Invalid app claim");

                        return Task.CompletedTask;
                    },
                };
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        foreach (var (key, value) in _previousEnv)
        {
            Environment.SetEnvironmentVariable(key, value);
        }

        base.Dispose(disposing);
    }

    private void SetEnv(string key, string value)
    {
        if (!_previousEnv.ContainsKey(key))
        {
            _previousEnv[key] = Environment.GetEnvironmentVariable(key);
        }

        Environment.SetEnvironmentVariable(key, value);
    }

    public string CreatePlatformToken(
        string userId = "platform-user",
        string displayName = "Platform User",
        DateTime? expiresUtc = null,
        string appClaim = AppClientId,
        SecurityKey? signingKey = null)
    {
        var key = signingKey ?? _signingKey;
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            audience: Audience,
            claims:
            [
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim("sub", userId),
                new Claim("name", displayName),
                new Claim("session_id", SessionId),
                new Claim("app", appClaim),
            ],
            expires: expiresUtc ?? DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private sealed class TestPlatformHttpClientFactory : IHttpClientFactory
    {
        private readonly Func<string> _tokenFactory;

        public TestPlatformHttpClientFactory(Func<string> tokenFactory) => _tokenFactory = tokenFactory;

        public HttpClient CreateClient(string name) => new(new GuestHandler(_tokenFactory));
    }

    private sealed class GuestHandler : HttpMessageHandler
    {
        private readonly Func<string> _tokenFactory;

        public GuestHandler(Func<string> tokenFactory) => _tokenFactory = tokenFactory;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Method != HttpMethod.Post || request.RequestUri?.AbsolutePath != "/auth/guest")
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));

            var response = new AuthResponse
            {
                AccessToken = _tokenFactory(),
                RefreshToken = "platform-refresh",
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(30).ToString("O"),
                Profile = new AccountMeResponse
                {
                    UserId = "platform-guest",
                    DisplayName = "Platform Guest",
                    IsGuest = true,
                    AvatarId = 1,
                    CreatedAt = DateTimeOffset.UtcNow.ToString("O"),
                },
            };

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(response),
            });
        }
    }

    private sealed class InMemoryUserProfileRepository : IUserProfileRepository
    {
        private readonly Dictionary<string, UserProfile> _profiles = new();

        public Task<UserProfile?> GetByIdAsync(string userId, CancellationToken ct)
        {
            _profiles.TryGetValue(userId, out var profile);
            return Task.FromResult(profile);
        }

        public Task UpsertAsync(string userId, string displayName, CancellationToken ct)
        {
            _profiles[userId] = new UserProfile
            {
                UserId = userId,
                DisplayName = displayName,
                AvatarId = 1,
                AccountCreatedAt = DateTimeOffset.UtcNow,
                LastLoginAt = DateTimeOffset.UtcNow,
            };
            return Task.CompletedTask;
        }
    }

    private sealed class TestSessionRepository : ISessionRepository
    {
        public Task<string?> GetCurrentSessionIdAsync(string userId, CancellationToken ct) => Task.FromResult<string?>(SessionId);
        public Task CreateSessionAsync(string userId, string sessionId, DateTimeOffset expiresAt, CancellationToken ct) => Task.CompletedTask;
        public Task<bool> TryCreateSessionAsync(string userId, string sessionId, DateTimeOffset expiresAt, CancellationToken ct) => Task.FromResult(true);
        public Task InvalidateAsync(string userId, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class TestSessionCache : ISessionCache
    {
        public Task<string?> GetSessionIdAsync(string userId) => Task.FromResult<string?>(SessionId);
        public Task SetSessionIdAsync(string userId, string sessionId, TimeSpan ttl) => Task.CompletedTask;
        public Task DeleteAsync(string userId) => Task.CompletedTask;
    }

    private sealed class TestStaminaRepository : IStaminaRepository
    {
        public Task<StaminaState> GetComputedAsync(string userId, int maxStamina, int rechargeIntervalMinutes, CancellationToken ct)
            => Task.FromResult(new StaminaState { UserId = userId, Current = maxStamina, LastRechargedAt = DateTimeOffset.UtcNow });

        public Task<StaminaState> DeductAsync(string userId, int maxStamina, int rechargeIntervalMinutes, CancellationToken ct)
            => Task.FromResult(new StaminaState { UserId = userId, Current = maxStamina - 1, LastRechargedAt = DateTimeOffset.UtcNow });

        public Task<(StaminaState state, int added)> AddAsync(string userId, int maxStamina, int rechargeIntervalMinutes, int toAdd, CancellationToken ct)
            => Task.FromResult((new StaminaState { UserId = userId, Current = maxStamina, LastRechargedAt = DateTimeOffset.UtcNow }, toAdd));
    }

    private sealed class TestCurrencyRepository : ICurrencyRepository
    {
        public Task<long> GetBalanceAsync(string userId, CancellationToken ct) => Task.FromResult(1200L);
        public Task<long> GrantAsync(string userId, long amount, string reason, string transactionId, string correlationId, CancellationToken ct) => Task.FromResult(1200L + amount);
        public Task<long> DeductAsync(string userId, long amount, string reason, string transactionId, string correlationId, CancellationToken ct) => Task.FromResult(1200L - amount);
    }

    private sealed class TestProgressRepository : IProgressRepository
    {
        public Task<IEnumerable<StageProgress>> GetAllAsync(string userId, CancellationToken ct)
            => Task.FromResult<IEnumerable<StageProgress>>(
            [
                new StageProgress { UserId = userId, StageId = 1, Stars = 3, ClearedAt = DateTimeOffset.UtcNow },
            ]);

        public Task UpsertBatchAsync(string userId, IEnumerable<StageProgress> records, CancellationToken ct) => Task.CompletedTask;
        public Task UpsertClearAsync(string userId, int stageId, int stars, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class TestDailyChallengeRepository : IDailyChallengeRepository
    {
        public Task<DailyChallengeProgress?> GetForDateAsync(string userId, DateOnly date, CancellationToken ct)
            => Task.FromResult<DailyChallengeProgress?>(new DailyChallengeProgress
            {
                UserId = userId,
                ChallengeDate = date,
                PlayCount = 1,
                Completed = false,
                StreakDays = 2,
                CreatedAt = DateTimeOffset.UtcNow,
            });

        public Task<int> IncrementPlayCountAsync(string userId, DateOnly date, CancellationToken ct) => Task.FromResult(1);
    }

    private sealed class TestStaticDataService : IStaticDataService
    {
        public IngameStageData? GetStage(int stageId) => GetAllStages().FirstOrDefault(s => s.StageId == stageId);
        public IngameItemData? GetItem(int itemId) => null;
        public IReadOnlyList<IngameStageData> GetAllStages() =>
        [
            new IngameStageData { StageId = 1, Width = 5, Height = 5 },
            new IngameStageData { StageId = 2, Width = 5, Height = 5 },
        ];
        public IReadOnlyList<IngameItemData> GetAllItems() => [];
        public OutgameStaminaConfigData GetStaminaConfig() => new() { ConfigId = 1, MaxStamina = 5, RechargeSeconds = 300 };
        public IReadOnlyList<OutgameAvatarData> GetAllAvatars() => [];
        public OutgameDailyChallengeData GetDailyChallengeConfig() => new() { ConfigId = 1, PlayCountTarget = 3, ResetHourUtc = 0, StagePickCount = 3 };
        public IReadOnlyList<OutgameDailyRewardData> GetAllDailyRewards() => [];
        public OutgameDailyRewardData? GetDailyReward(int streakDay) => null;
        public IReadOnlyList<OutgameShopCatalogData> GetShopCatalog() => [];
        public OutgameShopCatalogData? GetShopProduct(int productId) => null;
        public IReadOnlyList<OutgameSeasonEventData> GetAllSeasonEvents() => [];
    }
}
