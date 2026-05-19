using ProjectLink.Domain.StaticData;

namespace ProjectLink.Domain.Interfaces;

public interface IStaticDataService
{
    string MetaHash          { get; }
    string DataSchemaVersion { get; }
    string GetClientBundle();

    IngameStageData?               GetStage(int stageId);
    IngameItemData?                GetItem(int itemId);
    IReadOnlyList<IngameStageData> GetAllStages();
    IReadOnlyList<IngameItemData>  GetAllItems();

    OutgameStaminaConfigData             GetStaminaConfig();
    IReadOnlyList<OutgameAvatarData>     GetAllAvatars();
    IReadOnlyList<OutgameShopCatalogData> GetShopCatalog();
    OutgameShopCatalogData?              GetShopProduct(int productId);
    IReadOnlyList<OutgameSeasonEventData>   GetAllSeasonEvents();
    OutgameTimeExtendConfigData?            GetTimeExtendConfig(int extensionCount);

    StreakChallengeEventData?              GetStreakChallengeEvent(int eventId, int version);
    StreakChallengeEventData?              GetLatestEnabledStreakChallengeEvent();
    IReadOnlyList<StreakChallengeLevelData> GetStreakChallengeLevels(int eventId, int version);
    IReadOnlyList<StreakChallengeRewardItemData> GetStreakChallengeRewardItems(int rewardGroupId, int rewardGroupVersion);
}
