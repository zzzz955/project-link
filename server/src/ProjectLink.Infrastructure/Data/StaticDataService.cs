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
    private readonly IReadOnlyDictionary<int, OutgameShopCatalogData> _shopCatalog;
    private readonly IReadOnlyList<OutgameSeasonEventData>            _seasonEvents;
    private readonly IReadOnlyDictionary<int, OutgameTimeExtendConfigData> _timeExtendConfigs;
    private readonly IReadOnlyDictionary<(int, int), StreakChallengeEventData>         _streakEvents;
    private readonly IReadOnlyDictionary<(int, int), List<StreakChallengeLevelData>>   _streakLevels;
    private readonly IReadOnlyDictionary<(int, int), List<StreakChallengeRewardItemData>> _streakRewardItems;

    public StaticDataService(ILogger<StaticDataService> logger)
    {
        var basePath    = AppContext.BaseDirectory;
        var ingamePath  = Path.Combine(basePath, "generated", "data", "ingame");
        var outgamePath = Path.Combine(basePath, "generated", "data", "outgame");
        var streakPath  = Path.Combine(basePath, "generated", "data", "streak_challenge");

        _stages            = LoadStages(Path.Combine(ingamePath, "ingame_stage.csv"), logger);
        _items             = LoadItems(Path.Combine(ingamePath, "ingame_item.csv"), logger);
        _staminaConfig     = LoadStaminaConfig(Path.Combine(outgamePath, "outgame_stamina_config.csv"), logger);
        _avatars           = LoadAvatars(Path.Combine(outgamePath, "outgame_avatar.csv"), logger);
        _shopCatalog       = LoadShopCatalog(Path.Combine(outgamePath, "outgame_shop_catalog.csv"), logger);
        _seasonEvents      = LoadSeasonEvents(Path.Combine(outgamePath, "outgame_season_event.csv"), logger);
        _timeExtendConfigs = LoadTimeExtendConfigs(Path.Combine(outgamePath, "outgame_time_extend_config.csv"), logger);
        _streakEvents      = LoadStreakEvents(Path.Combine(streakPath, "streak_challenge_event.csv"), logger);
        _streakLevels      = LoadStreakLevels(Path.Combine(streakPath, "streak_challenge_level.csv"), logger);
        _streakRewardItems = LoadStreakRewardItems(Path.Combine(streakPath, "streak_challenge_reward_item.csv"), logger);
    }

    public IngameStageData?                      GetStage(int stageId)      => _stages.GetValueOrDefault(stageId);
    public IngameItemData?                       GetItem(int itemId)        => _items.GetValueOrDefault(itemId);
    public IReadOnlyList<IngameStageData>        GetAllStages()             => _stages.Values.ToList();
    public IReadOnlyList<IngameItemData>         GetAllItems()              => _items.Values.ToList();
    public OutgameStaminaConfigData              GetStaminaConfig()         => _staminaConfig;
    public IReadOnlyList<OutgameAvatarData>      GetAllAvatars()            => _avatars.Values.ToList();
    public IReadOnlyList<OutgameShopCatalogData> GetShopCatalog()           => _shopCatalog.Values.ToList();
    public OutgameShopCatalogData?               GetShopProduct(int id)     => _shopCatalog.GetValueOrDefault(id);
    public IReadOnlyList<OutgameSeasonEventData> GetAllSeasonEvents()            => _seasonEvents;
    public OutgameTimeExtendConfigData?          GetTimeExtendConfig(int count) => _timeExtendConfigs.GetValueOrDefault(count);

    public StreakChallengeEventData? GetStreakChallengeEvent(int eventId, int version)
        => _streakEvents.GetValueOrDefault((eventId, version));

    public StreakChallengeEventData? GetLatestEnabledStreakChallengeEvent()
        => _streakEvents.Values
            .Where(e => e.IsEnabled)
            .OrderByDescending(e => e.Version)
            .FirstOrDefault();

    public IReadOnlyList<StreakChallengeLevelData> GetStreakChallengeLevels(int eventId, int version)
        => _streakLevels.TryGetValue((eventId, version), out var list)
            ? list.OrderBy(l => l.LevelIndex).ToList()
            : new List<StreakChallengeLevelData>();

    public IReadOnlyList<StreakChallengeRewardItemData> GetStreakChallengeRewardItems(int rewardGroupId, int rewardGroupVersion)
        => _streakRewardItems.TryGetValue((rewardGroupId, rewardGroupVersion), out var list)
            ? list.OrderBy(i => i.DisplayOrder).ToList()
            : new List<StreakChallengeRewardItemData>();

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

    // outgame_time_extend_config: extensionCount,extendSeconds,costSoft
    private static IReadOnlyDictionary<int, OutgameTimeExtendConfigData> LoadTimeExtendConfigs(string path, ILogger logger)
    {
        var result = new Dictionary<int, OutgameTimeExtendConfigData>();
        if (!File.Exists(path)) { logger.LogWarning("outgame_time_extend_config.csv not found at {Path}", path); return result; }

        using var reader = new StreamReader(path);
        reader.ReadLine();
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cols = line.Split(',');
            var row = new OutgameTimeExtendConfigData
            {
                ExtensionCount = int.Parse(cols[0]),
                ExtendSeconds  = int.Parse(cols[1]),
                CostSoft       = int.Parse(cols[2]),
            };
            result[row.ExtensionCount] = row;
        }
        logger.LogInformation("Loaded {Count} time extend config rows from static data", result.Count);
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

    // streak_challenge_event: eventId,version,isEnabled,durationSeconds,resetType,stageCountPolicy,rewardPolicy,adPolicy
    private static IReadOnlyDictionary<(int, int), StreakChallengeEventData> LoadStreakEvents(string path, ILogger logger)
    {
        var result = new Dictionary<(int, int), StreakChallengeEventData>();
        if (!File.Exists(path)) { logger.LogWarning("streak_challenge_event.csv not found at {Path}", path); return result; }

        using var reader = new StreamReader(path);
        reader.ReadLine();
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var c = line.Split(',');
            var row = new StreakChallengeEventData
            {
                EventId          = int.Parse(c[0]),
                Version          = int.Parse(c[1]),
                IsEnabled        = bool.Parse(c[2]),
                DurationSeconds  = int.Parse(c[3]),
                ResetType        = c[4],
                StageCountPolicy = c[5],
                RewardPolicy     = c[6],
                AdPolicy         = c[7],
            };
            result[(row.EventId, row.Version)] = row;
        }
        logger.LogInformation("Loaded {Count} streak challenge events from static data", result.Count);
        return result;
    }

    // streak_challenge_level: eventId,version,levelIndex,requiredClearCount,rewardGroupId,allowTimeExtension,allowRevive,isEnabled,displayOrder,localizationKey
    private static IReadOnlyDictionary<(int, int), List<StreakChallengeLevelData>> LoadStreakLevels(string path, ILogger logger)
    {
        var result = new Dictionary<(int, int), List<StreakChallengeLevelData>>();
        if (!File.Exists(path)) { logger.LogWarning("streak_challenge_level.csv not found at {Path}", path); return result; }

        using var reader = new StreamReader(path);
        reader.ReadLine();
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var c = line.Split(',');
            var row = new StreakChallengeLevelData
            {
                EventId             = int.Parse(c[0]),
                Version             = int.Parse(c[1]),
                LevelIndex          = int.Parse(c[2]),
                RequiredClearCount  = int.Parse(c[3]),
                RewardGroupId       = int.Parse(c[4]),
                AllowTimeExtension  = bool.Parse(c[5]),
                AllowRevive         = bool.Parse(c[6]),
                IsEnabled           = bool.Parse(c[7]),
                DisplayOrder        = int.Parse(c[8]),
                LocalizationKey     = c[9],
            };
            var key = (row.EventId, row.Version);
            if (!result.ContainsKey(key)) result[key] = new List<StreakChallengeLevelData>();
            result[key].Add(row);
        }
        logger.LogInformation("Loaded streak challenge levels from static data");
        return result;
    }

    // streak_challenge_reward_item: rewardGroupId,rewardGroupVersion,itemType,itemId,amount,weight,displayOrder
    private static IReadOnlyDictionary<(int, int), List<StreakChallengeRewardItemData>> LoadStreakRewardItems(string path, ILogger logger)
    {
        var result = new Dictionary<(int, int), List<StreakChallengeRewardItemData>>();
        if (!File.Exists(path)) { logger.LogWarning("streak_challenge_reward_item.csv not found at {Path}", path); return result; }

        using var reader = new StreamReader(path);
        reader.ReadLine();
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var c = line.Split(',');
            var row = new StreakChallengeRewardItemData
            {
                RewardGroupId      = int.Parse(c[0]),
                RewardGroupVersion = int.Parse(c[1]),
                ItemType           = c[2],
                ItemId             = int.Parse(c[3]),
                Amount             = int.Parse(c[4]),
                Weight             = int.Parse(c[5]),
                DisplayOrder       = int.Parse(c[6]),
            };
            var key = (row.RewardGroupId, row.RewardGroupVersion);
            if (!result.ContainsKey(key)) result[key] = new List<StreakChallengeRewardItemData>();
            result[key].Add(row);
        }
        logger.LogInformation("Loaded streak challenge reward items from static data");
        return result;
    }
}
