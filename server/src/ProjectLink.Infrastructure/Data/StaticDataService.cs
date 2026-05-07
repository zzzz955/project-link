using Microsoft.Extensions.Logging;
using ProjectLink.Domain.Interfaces;
using ProjectLink.Domain.StaticData;

namespace ProjectLink.Infrastructure.Data;

public class StaticDataService : IStaticDataService
{
    private readonly IReadOnlyDictionary<int, IngameStageData>       _stages;
    private readonly IReadOnlyDictionary<int, IngameItemData>        _items;
    private readonly OutgameStaminaConfigData                         _staminaConfig;
    private readonly IReadOnlyDictionary<int, OutgameAvatarData>     _avatars;
    private readonly OutgameDailyChallengeData                        _dailyChallengeConfig;
    private readonly IReadOnlyDictionary<int, OutgameDailyRewardData> _dailyRewards;
    private readonly IReadOnlyDictionary<int, OutgameShopCatalogData> _shopCatalog;
    private readonly IReadOnlyList<OutgameSeasonEventData>            _seasonEvents;

    public StaticDataService(ILogger<StaticDataService> logger)
    {
        var basePath = AppContext.BaseDirectory;
        var ingamePath  = Path.Combine(basePath, "generated", "data", "ingame");
        var outgamePath = Path.Combine(basePath, "generated", "data", "outgame");

        _stages               = LoadStages(Path.Combine(ingamePath, "ingame_stage.csv"), logger);
        _items                = LoadItems(Path.Combine(ingamePath, "ingame_item.csv"), logger);
        _staminaConfig        = LoadStaminaConfig(Path.Combine(outgamePath, "outgame_stamina_config.csv"), logger);
        _avatars              = LoadAvatars(Path.Combine(outgamePath, "outgame_avatar.csv"), logger);
        _dailyChallengeConfig = LoadDailyChallengeConfig(Path.Combine(outgamePath, "outgame_daily_challenge.csv"), logger);
        _dailyRewards         = LoadDailyRewards(Path.Combine(outgamePath, "outgame_daily_reward.csv"), logger);
        _shopCatalog          = LoadShopCatalog(Path.Combine(outgamePath, "outgame_shop_catalog.csv"), logger);
        _seasonEvents         = LoadSeasonEvents(Path.Combine(outgamePath, "outgame_season_event.csv"), logger);
    }

    public IngameStageData?                     GetStage(int stageId)      => _stages.GetValueOrDefault(stageId);
    public IngameItemData?                      GetItem(int itemId)        => _items.GetValueOrDefault(itemId);
    public IReadOnlyList<IngameStageData>       GetAllStages()             => _stages.Values.ToList();
    public IReadOnlyList<IngameItemData>        GetAllItems()              => _items.Values.ToList();
    public OutgameStaminaConfigData             GetStaminaConfig()         => _staminaConfig;
    public IReadOnlyList<OutgameAvatarData>     GetAllAvatars()            => _avatars.Values.ToList();
    public OutgameDailyChallengeData            GetDailyChallengeConfig()  => _dailyChallengeConfig;
    public IReadOnlyList<OutgameDailyRewardData> GetAllDailyRewards()      => _dailyRewards.Values.ToList();
    public OutgameDailyRewardData?              GetDailyReward(int day)    => _dailyRewards.GetValueOrDefault(day);
    public IReadOnlyList<OutgameShopCatalogData> GetShopCatalog()          => _shopCatalog.Values.ToList();
    public OutgameShopCatalogData?              GetShopProduct(int id)     => _shopCatalog.GetValueOrDefault(id);
    public IReadOnlyList<OutgameSeasonEventData> GetAllSeasonEvents()      => _seasonEvents;

    // ingame_stage: stageId,width,height,timeLimit,moveLimit,difficulty,boardEncoding,nodeMap,cellMap,soft_reward,stageMeta...,generatorSeed
    private static IReadOnlyDictionary<int, IngameStageData> LoadStages(string path, ILogger logger)
    {
        var result = new Dictionary<int, IngameStageData>();
        if (!File.Exists(path)) { logger.LogWarning("ingame_stage.csv not found at {Path}", path); return result; }

        using var reader = new StreamReader(path);
        reader.ReadLine();
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cols = line.Split(',');
            result[int.Parse(cols[0])] = new IngameStageData
            {
                StageId       = int.Parse(cols[0]),
                Width         = int.Parse(cols[1]),
                Height        = int.Parse(cols[2]),
                TimeLimit     = int.Parse(cols[3]),
                MoveLimit     = int.Parse(cols[4]),
                Difficulty    = int.Parse(cols[5]),
                BoardEncoding = cols[6],
                NodeMap       = cols[7],
                CellMap       = cols[8],
                SoftReward    = int.Parse(cols[9]),
                StageMeta     = string.Join(",", cols[10..^1]),
                GeneratorSeed = uint.Parse(cols[^1]),
            };
        }
        logger.LogInformation("Loaded {Count} stages from static data", result.Count);
        return result;
    }

    // ingame_item: itemId,name,type,costSoft,description
    private static IReadOnlyDictionary<int, IngameItemData> LoadItems(string path, ILogger logger)
    {
        var result = new Dictionary<int, IngameItemData>();
        if (!File.Exists(path)) { logger.LogWarning("ingame_item.csv not found at {Path}", path); return result; }

        using var reader = new StreamReader(path);
        reader.ReadLine();
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cols = line.Split(',');
            result[int.Parse(cols[0])] = new IngameItemData
            {
                ItemId      = int.Parse(cols[0]),
                Name        = cols[1],
                Type        = cols[2],
                CostSoft    = int.Parse(cols[3]),
                Description = string.Join(",", cols[4..]),
            };
        }
        logger.LogInformation("Loaded {Count} items from static data", result.Count);
        return result;
    }

    // outgame_stamina_config: configId,maxStamina,rechargeSeconds,refillCostSoft,adRewardAmount
    private static OutgameStaminaConfigData LoadStaminaConfig(string path, ILogger logger)
    {
        var fallback = new OutgameStaminaConfigData { ConfigId = 1, MaxStamina = 5, RechargeSeconds = 300, RefillCostSoft = 80, AdRewardAmount = 2 };
        if (!File.Exists(path)) { logger.LogWarning("outgame_stamina_config.csv not found at {Path}", path); return fallback; }

        using var reader = new StreamReader(path);
        reader.ReadLine();
        var line = reader.ReadLine();
        if (string.IsNullOrWhiteSpace(line)) return fallback;
        var cols = line.Split(',');
        return new OutgameStaminaConfigData
        {
            ConfigId        = int.Parse(cols[0]),
            MaxStamina      = int.Parse(cols[1]),
            RechargeSeconds = int.Parse(cols[2]),
            RefillCostSoft  = int.Parse(cols[3]),
            AdRewardAmount  = int.Parse(cols[4]),
        };
    }

    // outgame_avatar: id,name,iconPath,unlockCondition
    private static IReadOnlyDictionary<int, OutgameAvatarData> LoadAvatars(string path, ILogger logger)
    {
        var result = new Dictionary<int, OutgameAvatarData>();
        if (!File.Exists(path)) { logger.LogWarning("outgame_avatar.csv not found at {Path}", path); return result; }

        using var reader = new StreamReader(path);
        reader.ReadLine();
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cols = line.Split(',');
            result[int.Parse(cols[0])] = new OutgameAvatarData
            {
                Id               = int.Parse(cols[0]),
                Name             = cols[1],
                IconPath         = cols[2],
                UnlockCondition  = cols[3],
            };
        }
        logger.LogInformation("Loaded {Count} avatars from static data", result.Count);
        return result;
    }

    // outgame_daily_challenge: configId,playCountTarget,resetHourUtc
    private static OutgameDailyChallengeData LoadDailyChallengeConfig(string path, ILogger logger)
    {
        var fallback = new OutgameDailyChallengeData { ConfigId = 1, PlayCountTarget = 3, ResetHourUtc = 0 };
        if (!File.Exists(path)) { logger.LogWarning("outgame_daily_challenge.csv not found at {Path}", path); return fallback; }

        using var reader = new StreamReader(path);
        reader.ReadLine();
        var line = reader.ReadLine();
        if (string.IsNullOrWhiteSpace(line)) return fallback;
        var cols = line.Split(',');
        return new OutgameDailyChallengeData
        {
            ConfigId        = int.Parse(cols[0]),
            PlayCountTarget = int.Parse(cols[1]),
            ResetHourUtc    = int.Parse(cols[2]),
        };
    }

    // outgame_daily_reward: streakDay,rewardType,rewardId,amount
    private static IReadOnlyDictionary<int, OutgameDailyRewardData> LoadDailyRewards(string path, ILogger logger)
    {
        var result = new Dictionary<int, OutgameDailyRewardData>();
        if (!File.Exists(path)) { logger.LogWarning("outgame_daily_reward.csv not found at {Path}", path); return result; }

        using var reader = new StreamReader(path);
        reader.ReadLine();
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cols = line.Split(',');
            result[int.Parse(cols[0])] = new OutgameDailyRewardData
            {
                StreakDay  = int.Parse(cols[0]),
                RewardType = cols[1],
                RewardId   = int.Parse(cols[2]),
                Amount     = int.Parse(cols[3]),
            };
        }
        logger.LogInformation("Loaded {Count} daily rewards from static data", result.Count);
        return result;
    }

    // outgame_shop_catalog: productId,category,name,grantItemId,grantQuantity,priceSoft,priceIapSku,sortOrder,isEnabled
    private static IReadOnlyDictionary<int, OutgameShopCatalogData> LoadShopCatalog(string path, ILogger logger)
    {
        var result = new Dictionary<int, OutgameShopCatalogData>();
        if (!File.Exists(path)) { logger.LogWarning("outgame_shop_catalog.csv not found at {Path}", path); return result; }

        using var reader = new StreamReader(path);
        reader.ReadLine();
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cols = line.Split(',');
            result[int.Parse(cols[0])] = new OutgameShopCatalogData
            {
                ProductId     = int.Parse(cols[0]),
                Category      = cols[1],
                Name          = cols[2],
                GrantItemId   = int.Parse(cols[3]),
                GrantQuantity = int.Parse(cols[4]),
                PriceSoft     = int.Parse(cols[5]),
                PriceIapSku   = string.IsNullOrEmpty(cols[6]) ? null : cols[6],
                SortOrder     = int.Parse(cols[7]),
                IsEnabled     = bool.Parse(cols[8]),
            };
        }
        logger.LogInformation("Loaded {Count} shop products from static data", result.Count);
        return result;
    }

    // outgame_season_event: eventId,name,type,startAt,endAt,metricLabel,rankingMetric
    private static IReadOnlyList<OutgameSeasonEventData> LoadSeasonEvents(string path, ILogger logger)
    {
        var result = new List<OutgameSeasonEventData>();
        if (!File.Exists(path)) { logger.LogWarning("outgame_season_event.csv not found at {Path}", path); return result; }

        using var reader = new StreamReader(path);
        reader.ReadLine();
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cols = line.Split(',');
            result.Add(new OutgameSeasonEventData
            {
                EventId       = int.Parse(cols[0]),
                Name          = cols[1],
                Type          = cols[2],
                StartAt       = cols[3],
                EndAt         = cols[4],
                MetricLabel   = cols[5],
                RankingMetric = cols[6],
            });
        }
        logger.LogInformation("Loaded {Count} season events from static data", result.Count);
        return result;
    }
}
