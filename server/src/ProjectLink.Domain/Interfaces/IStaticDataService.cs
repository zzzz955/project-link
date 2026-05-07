using ProjectLink.Domain.StaticData;

namespace ProjectLink.Domain.Interfaces;

public interface IStaticDataService
{
    IngameStageData?             GetStage(int stageId);
    IngameItemData?              GetItem(int itemId);
    IReadOnlyList<IngameStageData> GetAllStages();
}
