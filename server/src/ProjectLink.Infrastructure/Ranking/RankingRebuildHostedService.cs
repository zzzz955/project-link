using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectLink.Application.Ranking;
using StackExchange.Redis;

namespace ProjectLink.Infrastructure.Ranking;

public class RankingRebuildHostedService : IHostedService
{
    private readonly IServiceProvider      _services;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RankingRebuildHostedService> _logger;

    public RankingRebuildHostedService(IServiceProvider services, IConnectionMultiplexer redis, ILogger<RankingRebuildHostedService> logger)
    {
        _services = services;
        _redis    = redis;
        _logger   = logger;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        var exists = await db.KeyExistsAsync("ranking:global:score");
        if (exists) return;

        _logger.LogInformation("Ranking keys absent — rebuilding from DB");

        using var scope   = _services.CreateScope();
        var rankingService = scope.ServiceProvider.GetRequiredService<RankingService>();

        await rankingService.RebuildFromDbAsync(ct);
        _logger.LogInformation("Ranking rebuild complete");
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
