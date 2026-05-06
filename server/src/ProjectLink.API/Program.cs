using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProjectLink.Application.Progress;
using ProjectLink.Application.Session;
using ProjectLink.Domain.Interfaces;
using ProjectLink.Infrastructure.Cache;
using ProjectLink.Infrastructure.Persistence;
using ProjectLink.Infrastructure.Security;
using Scalar.AspNetCore;
using StackExchange.Redis;
using ProjectLink.API.Middleware;

var builder = WebApplication.CreateBuilder(args);
var config  = builder.Configuration;

// 1. DbContext
builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseNpgsql(config.GetConnectionString("Postgres")));

// 2. Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(config["Redis:Connection"] ?? "localhost:6379"));

// 3. Repositories + SessionService + Application handlers
builder.Services.AddScoped<IProgressRepository, ProgressRepository>();
builder.Services.AddScoped<ISessionRepository,  SessionRepository>();
builder.Services.AddScoped<ISessionCache,        RedisSessionCache>();
builder.Services.AddScoped<SessionService>();
builder.Services.AddScoped<GetProgressQueryHandler>();
builder.Services.AddScoped<UpsertProgressCommandHandler>();

// 4. JwtPublicKeyCache as hosted service
builder.Services.AddHttpClient<JwtPublicKeyCache>();
builder.Services.AddSingleton<JwtPublicKeyCache>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<JwtPublicKeyCache>());

// 5. JWT Bearer auth
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

// Configure JWT options after the container is built so we can resolve JwtPublicKeyCache
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<JwtPublicKeyCache, IConfiguration>((opts, keyCache, cfg) =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKeyResolver = (_, _, _, _) => keyCache.GetKeys(),
            ValidateIssuer           = false,
            ValidateAudience         = true,
            ValidAudience            = cfg["Jwt:Audience"],
        };

        opts.Events = new JwtBearerEvents
        {
            OnTokenValidated = ctx =>
            {
                var clientId = cfg["App:ClientId"];
                var appClaim = ctx.Principal?.FindFirst("app")?.Value;
                if (appClaim != clientId)
                    ctx.Fail("Invalid app claim");
                return Task.CompletedTask;
            },
        };
    });

builder.Services.AddAuthorization();

// 6. Scalar / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

var app = builder.Build();

// 7. Middleware pipeline
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
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

// 8. Controllers
app.MapControllers();

app.Run();
