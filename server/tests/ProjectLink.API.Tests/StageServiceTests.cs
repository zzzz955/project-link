using ProjectLink.Application.Stage;
using ProjectLink.Domain.Entities;
using ProjectLink.Domain.Exceptions;
using ProjectLink.Domain.Interfaces;
using ProjectLink.Domain.Stage;
using ProjectLink.Domain.StaticData;
using Xunit;

namespace ProjectLink.API.Tests;

public sealed class StageServiceTests
{
    [Fact]
    public async Task StartAsync_ReplacesActiveSessionWithNewPaidAttempt()
    {
        var cache = new TestStageSessionCache
        {
            Current = new StageSession
            {
                UserId = "user-1",
                StageId = 1,
                Token = "old-token",
                StartAtMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            },
        };
        var stamina = new TestStaminaRepository();
        var service = CreateService(cache, stamina);

        var response = await service.StartAsync("user-1", 1, CancellationToken.None);

        Assert.Equal(1, stamina.DeductCount);
        Assert.NotEqual("old-token", response.SessionToken);
        Assert.Equal(response.SessionToken, cache.Current!.Token);
        Assert.Equal(1, cache.Current.StageId);
    }

    [Fact]
    public async Task StartAsync_InvalidatesPreviousSessionToken()
    {
        var cache = new TestStageSessionCache
        {
            Current = new StageSession
            {
                UserId = "user-1",
                StageId = 1,
                Token = "old-token",
                StartAtMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            },
        };
        var service = CreateService(cache, new TestStaminaRepository());

        var response = await service.StartAsync("user-1", 1, CancellationToken.None);

        await Assert.ThrowsAsync<StageSessionNotFoundException>(() =>
            service.EndAsync("user-1", 1, "old-token", "fail", 0, 0, "test", CancellationToken.None));

        await service.EndAsync("user-1", 1, response.SessionToken, "fail", 0, 0, "test", CancellationToken.None);
        Assert.Null(cache.Current);
    }

    static StageService CreateService(TestStageSessionCache cache, TestStaminaRepository stamina)
        => new(
            cache,
            stamina,
            new TestInventoryRepository(),
            new TestStaticDataService(),
            null!,
            new TestStageEndTransaction());

    sealed class TestStageSessionCache : IStageSessionCache
    {
        public StageSession? Current { get; set; }

        public Task<StageSession?> GetAsync(string userId, CancellationToken ct)
            => Task.FromResult(Current);

        public Task SetAsync(string userId, StageSession session, TimeSpan ttl, CancellationToken ct)
        {
            Current = session;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string userId, CancellationToken ct)
        {
            Current = null;
            return Task.CompletedTask;
        }
    }

    sealed class TestStaminaRepository : IStaminaRepository
    {
        public int DeductCount { get; private set; }

        public Task<StaminaState> GetComputedAsync(string userId, int maxStamina, int rechargeIntervalMinutes, CancellationToken ct)
            => Task.FromResult(new StaminaState { UserId = userId, Current = maxStamina, LastRechargedAt = DateTimeOffset.UtcNow });

        public Task<StaminaState> DeductAsync(string userId, int maxStamina, int rechargeIntervalMinutes, CancellationToken ct)
        {
            DeductCount++;
            return Task.FromResult(new StaminaState { UserId = userId, Current = maxStamina - DeductCount, LastRechargedAt = DateTimeOffset.UtcNow });
        }

        public Task<(StaminaState state, int added)> AddAsync(string userId, int maxStamina, int rechargeIntervalMinutes, int toAdd, CancellationToken ct)
            => Task.FromResult((new StaminaState { UserId = userId, Current = maxStamina, LastRechargedAt = DateTimeOffset.UtcNow }, toAdd));
    }

    sealed class TestInventoryRepository : IInventoryRepository
    {
        public Task<List<Inventory>> GetAllAsync(string userId, CancellationToken ct) => Task.FromResult(new List<Inventory>());
        public Task<int> GrantAsync(string userId, int itemId, int quantity, CancellationToken ct) => Task.FromResult(quantity);
        public Task<int> DeductAsync(string userId, int itemId, int quantity, CancellationToken ct) => Task.FromResult(0);
    }

    sealed class TestStaticDataService : IStaticDataService
    {
        public IngameStageData? GetStage(int stageId) => GetAllStages().FirstOrDefault(x => x.StageId == stageId);
        public IngameItemData? GetItem(int itemId) => null;
        public IReadOnlyList<IngameStageData> GetAllStages() => [new IngameStageData { StageId = 1, Width = 5, Height = 5, TimeLimit = 60 }];
        public IReadOnlyList<IngameItemData> GetAllItems() => [];
        public OutgameStaminaConfigData GetStaminaConfig() => new() { ConfigId = 1, MaxStamina = 5, RechargeSeconds = 300 };
        public IReadOnlyList<OutgameAvatarData> GetAllAvatars() => [];
        public OutgameDailyChallengeData GetDailyChallengeConfig() => new() { ConfigId = 1, PlayCountTarget = 3, ResetHourUtc = 0, StagePickCount = 1 };
        public IReadOnlyList<OutgameDailyRewardData> GetAllDailyRewards() => [];
        public OutgameDailyRewardData? GetDailyReward(int streakDay) => null;
        public IReadOnlyList<OutgameShopCatalogData> GetShopCatalog() => [];
        public OutgameShopCatalogData? GetShopProduct(int productId) => null;
        public IReadOnlyList<OutgameSeasonEventData> GetAllSeasonEvents() => [];
    }

    sealed class TestStageEndTransaction : IStageEndTransaction
    {
        public Task<StageEndDbResult> ExecuteAsync(StageEndDbCommand command, CancellationToken ct)
            => Task.FromResult(new StageEndDbResult());
    }
}
