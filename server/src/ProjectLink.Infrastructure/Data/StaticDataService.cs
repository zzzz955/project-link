using Microsoft.Extensions.Logging;
using ProjectLink.Domain.Interfaces;
using ProjectLink.Domain.StaticData;

namespace ProjectLink.Infrastructure.Data;

public class StaticDataService : IStaticDataService
{
    private readonly IReadOnlyDictionary<int, IngameStageData> _stages;
    private readonly IReadOnlyDictionary<int, IngameItemData>  _items;

    public StaticDataService(ILogger<StaticDataService> logger)
    {
        var basePath = AppContext.BaseDirectory;
        _stages = LoadStages(Path.Combine(basePath, "generated", "data", "ingame", "ingame_stage.csv"), logger);
        _items  = LoadItems(Path.Combine(basePath, "generated", "data", "ingame", "ingame_item.csv"), logger);
    }

    public IngameStageData?             GetStage(int stageId)  => _stages.GetValueOrDefault(stageId);
    public IngameItemData?              GetItem(int itemId)    => _items.GetValueOrDefault(itemId);
    public IReadOnlyList<IngameStageData> GetAllStages()       => _stages.Values.ToList();

    private static IReadOnlyDictionary<int, IngameStageData> LoadStages(string path, ILogger logger)
    {
        var result = new Dictionary<int, IngameStageData>();
        if (!File.Exists(path))
        {
            logger.LogWarning("ingame_stage.csv not found at {Path}", path);
            return result;
        }

        using var reader = new StreamReader(path);
        reader.ReadLine(); // skip header row

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cols = line.Split(',');
            // stageMeta may contain commas — reconstruct from cols[8] to cols[^2], generatorSeed is last
            var stage = new IngameStageData
            {
                StageId       = int.Parse(cols[0]),
                Width         = int.Parse(cols[1]),
                Height        = int.Parse(cols[2]),
                TimeLimit     = int.Parse(cols[3]),
                Difficulty    = int.Parse(cols[4]),
                BoardEncoding = cols[5],
                NodeMap       = cols[6],
                CellMap       = cols[7],
                SoftReward    = int.Parse(cols[8]),
                StageMeta     = string.Join(",", cols[9..^1]),
                GeneratorSeed = uint.Parse(cols[^1]),
            };
            result[stage.StageId] = stage;
        }

        logger.LogInformation("Loaded {Count} stages from static data", result.Count);
        return result;
    }

    private static IReadOnlyDictionary<int, IngameItemData> LoadItems(string path, ILogger logger)
    {
        var result = new Dictionary<int, IngameItemData>();
        if (!File.Exists(path))
        {
            logger.LogWarning("ingame_item.csv not found at {Path}. Run npm run gen:data.", path);
            return result;
        }

        using var reader = new StreamReader(path);
        reader.ReadLine(); // skip header row

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cols = line.Split(',');
            var item = new IngameItemData
            {
                ItemId      = int.Parse(cols[0]),
                Name        = cols[1],
                Type        = cols[2],
                CostSoft    = int.Parse(cols[3]),
                Description = string.Join(",", cols[4..]),
            };
            result[item.ItemId] = item;
        }

        logger.LogInformation("Loaded {Count} items from static data", result.Count);
        return result;
    }
}
