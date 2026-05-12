using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProjectLink.Application.Bootstrap;
using ProjectLink.Application.Currency;
using ProjectLink.Application.DailyChallenge;
using ProjectLink.Application.Inventory;
using ProjectLink.Application.Lobby;
using ProjectLink.Application.Progress;
using ProjectLink.Application.Ranking;
using ProjectLink.Application.Reward;
using ProjectLink.Application.Session;
using ProjectLink.Application.Settings;
using ProjectLink.Application.Shop;
using ProjectLink.Application.Stamina;
using ProjectLink.Application.Stage;
using ProjectLink.Application.UserProfile;
using ProjectLink.Infrastructure.Ranking;
using ProjectLink.Contracts.Common;
using ProjectLink.Domain.Interfaces;
using ProjectLink.Infrastructure.Cache;
using ProjectLink.Infrastructure.Data;
using ProjectLink.Infrastructure.Persistence;
using ProjectLink.Infrastructure.Security;
using Scalar.AspNetCore;
using StackExchange.Redis;
using ProjectLink.API.Middleware;
using ProjectLink.API;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.With<ShortSourceContextEnricher>());

var logNormalRequests = builder.Environment.IsDevelopment();
var config  = builder.Configuration;
var appConfig = ProjectLinkConfiguration.Load(config);

// 1. DbContext
builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseMySql(appConfig.Database.ConnectionString, new MySqlServerVersion(new Version(8, 0, 36))));

// 2. Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(appConfig.Redis.ConnectionString));

// 3. Static data — singleton loaded once at startup
builder.Services.AddSingleton<IStaticDataService, StaticDataService>();

// 4. Repositories
builder.Services.AddScoped<IProgressRepository,             ProgressRepository>();
builder.Services.AddScoped<ISessionRepository,              SessionRepository>();
builder.Services.AddScoped<ISessionCache,                   RedisSessionCache>();
builder.Services.AddScoped<IUserProfileRepository,          UserProfileRepository>();
builder.Services.AddScoped<ICurrencyRepository,             CurrencyRepository>();
builder.Services.AddScoped<IStaminaRepository,              StaminaRepository>();
builder.Services.AddScoped<IInventoryRepository,            InventoryRepository>();
builder.Services.AddScoped<IRankingRepository,              RankingRepository>();
builder.Services.AddScoped<IStageSessionCache,              StageSessionCache>();
builder.Services.AddScoped<IDailyChallengeRepository,       DailyChallengeRepository>();
builder.Services.AddScoped<IPlayerSettingsRepository,       PlayerSettingsRepository>();
builder.Services.AddScoped<IStageEndTransaction,            StageEndTransactionRepository>();
builder.Services.AddScoped<IStaminaRefillTransaction,       StaminaRefillTransactionRepository>();
builder.Services.AddScoped<IDailyChallengeCompleteTransaction, DailyChallengeCompleteTransactionRepository>();
builder.Services.AddScoped<IShopPurchaseTransaction,        ShopPurchaseTransactionRepository>();

// 5. Application services
builder.Services.AddScoped<SessionService>();
builder.Services.AddScoped<UserProfileService>();
builder.Services.AddScoped<CurrencyService>();
builder.Services.AddScoped<StaminaService>();
builder.Services.AddScoped<InventoryService>();
builder.Services.AddScoped<RankingService>();
builder.Services.AddScoped<StageService>();
builder.Services.AddScoped<GetProgressQueryHandler>();
builder.Services.AddScoped<BootstrapService>();
builder.Services.AddScoped<LobbyService>();
builder.Services.AddScoped<DailyChallengeService>();
builder.Services.AddScoped<ShopService>();
builder.Services.AddScoped<PlayerSettingsService>();
builder.Services.AddScoped<RewardService>();

// 6. JwtPublicKeyCache + Ranking rebuild as hosted services
builder.Services.AddHttpClient<JwtPublicKeyCache>();
builder.Services.AddSingleton<JwtPublicKeyCache>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<JwtPublicKeyCache>());
builder.Services.AddHostedService<RankingRebuildHostedService>();

// 7. Auth
builder.Services.AddSingleton(appConfig);

var useMockAuth = appConfig.Auth.UseMock;
if (useMockAuth)
{
    builder.Services.AddAuthentication(MockAuthenticationHandler.SchemeName)
        .AddScheme<AuthenticationSchemeOptions, MockAuthenticationHandler>(MockAuthenticationHandler.SchemeName, _ => { });
}
else
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer();

    builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
        .Configure<JwtPublicKeyCache, IConfiguration>((opts, keyCache, cfg) =>
        {
            opts.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKeyResolver = (_, _, _, _) => keyCache.GetKeys(),
                ValidateIssuer           = false,
                ValidateAudience         = true,
                ValidAudience            = appConfig.Auth.JwtAudience,
            };

            opts.Events = new JwtBearerEvents
            {
                OnTokenValidated = ctx =>
                {
                    var clientId = appConfig.App.ClientId;
                    var appClaim = ctx.Principal?.FindFirst("app")?.Value;
                    if (appClaim != clientId)
                        ctx.Fail("Invalid app claim");
                    return Task.CompletedTask;
                },
            };
        });
}

builder.Services.AddAuthorization();

// 8. Rate limiting
builder.Services.AddRateLimiter(opts =>
{
    // Global IP-based DDoS protection: 500 req/min per IP
    opts.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 500,
                Window      = TimeSpan.FromMinutes(1),
                QueueLimit  = 0
            }));

    // Stage start: 720/hour per user (anti-bot; ~10s/stage × 2× buffer)
    opts.AddPolicy("stage_start", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                          ?? ctx.Connection.RemoteIpAddress?.ToString()
                          ?? "anon",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = appConfig.RateLimit.StageStartPerHour,
                Window      = TimeSpan.FromHours(1),
                QueueLimit  = 0
            }));

    // Ranking: 60/min per user (scraping prevention)
    opts.AddPolicy("ranking", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                          ?? ctx.Connection.RemoteIpAddress?.ToString()
                          ?? "anon",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = appConfig.RateLimit.RankingPerMinute,
                Window      = TimeSpan.FromMinutes(1),
                QueueLimit  = 0
            }));

    opts.RejectionStatusCode = 429;
    opts.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsJsonAsync(
            new ErrorResponse { ErrorCode = "RATE_LIMITED" }, token);
    };
});

// 9. Scalar / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

var app = builder.Build();

// 10. Middleware pipeline
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    options.GetLevel = (context, elapsed, exception) =>
    {
        if (exception is not null || context.Response.StatusCode >= 500)
            return LogEventLevel.Error;

        if (elapsed > 500)
            return LogEventLevel.Warning;

        return logNormalRequests ? LogEventLevel.Information : LogEventLevel.Debug;
    };
    options.EnrichDiagnosticContext = (diagnosticContext, context) =>
    {
        if (context.Items["CorrelationId"] is string correlationId)
            diagnosticContext.Set("CorrelationId", correlationId);
    };
});
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.UseMiddleware<VersionCheckMiddleware>();
app.UseMiddleware<MetaHashMiddleware>();
app.UseMiddleware<SessionValidationMiddleware>();

app.UseSwagger(options =>
{
    options.RouteTemplate = "openapi/{documentName}.json";
});
app.MapScalarApiReference(options =>
{
    options.WithOpenApiRoutePattern("/openapi/{documentName}.json");
});

// 11. Controllers
app.MapControllers();

app.Run();

public partial class Program;
