using ProjectLink.Domain.StaticData;

namespace ProjectLink.Domain.Interfaces;

public interface IStaticDataService
{
    IngameStageData?               GetStage(int stageId);
    IngameItemData?                GetItem(int itemId);
    IReadOnlyList<IngameStageData> GetAllStages();
    IReadOnlyList<IngameItemData>  GetAllItems();

    OutgameStaminaConfigData       GetStaminaConfig();
    IReadOnlyList<OutgameAvatarData> GetAllAvatars();
    OutgameDailyChallengeData      GetDailyChallengeConfig();
    IReadOnlyList<OutgameDailyRewardData> GetAllDailyRewards();
    OutgameDailyRewardData?        GetDailyReward(int streakDay);
    IReadOnlyList<OutgameShopCatalogData> GetShopCatalog();
    OutgameShopCatalogData?        GetShopProduct(int productId);
    IReadOnlyList<OutgameSeasonEventData> GetAllSeasonEvents();
}
