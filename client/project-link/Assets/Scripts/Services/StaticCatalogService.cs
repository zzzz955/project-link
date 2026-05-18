using System.Collections.Generic;
using ProjectLink.Data;
using ProjectLink.Data.Generated;

namespace ProjectLink.Services
{
    public interface IStaticCatalogService
    {
        IReadOnlyList<OutgameShopCatalog> GetEnabledShopProducts(string category = null);
        IReadOnlyList<OutgameSeasonEvent> SeasonEvents { get; }
        OutgameStaminaConfig StaminaConfig { get; }
        IReadOnlyList<OutgameDailyReward> DailyRewards { get; }
        IReadOnlyList<IngameItem> GetAllItems();
        IReadOnlyList<StreakChallengeLevel> GetStreakChallengeLevels(int eventId = 0, int version = 0);
        IReadOnlyList<StreakChallengeRewardItem> GetStreakChallengeRewardItems(int rewardGroupId, int rewardGroupVersion = 1);
        IngameItem FindItem(int itemId);
        OutgameAvatar FindAvatar(int avatarId);
        OutgameSeasonEvent FindSeasonEvent(int eventId);
    }

    public sealed class StaticCatalogService : IStaticCatalogService
    {
        public IReadOnlyList<OutgameSeasonEvent> SeasonEvents => OutgameDataLoader.SeasonEvents;
        public OutgameStaminaConfig StaminaConfig => OutgameDataLoader.StaminaConfig;
        public IReadOnlyList<OutgameDailyReward> DailyRewards => OutgameDataLoader.DailyRewards;

        public IReadOnlyList<OutgameShopCatalog> GetEnabledShopProducts(string category = null)
        {
            return OutgameDataLoader.GetEnabledShopProducts(category);
        }

        public IReadOnlyList<IngameItem> GetAllItems()
        {
            return OutgameDataLoader.GetAllItems();
        }

        public IReadOnlyList<StreakChallengeLevel> GetStreakChallengeLevels(int eventId = 0, int version = 0)
        {
            return OutgameDataLoader.GetStreakChallengeLevels(eventId, version);
        }

        public IReadOnlyList<StreakChallengeRewardItem> GetStreakChallengeRewardItems(int rewardGroupId, int rewardGroupVersion = 1)
        {
            return OutgameDataLoader.GetStreakChallengeRewardItems(rewardGroupId, rewardGroupVersion);
        }

        public IngameItem FindItem(int itemId)
        {
            return OutgameDataLoader.FindItem(itemId);
        }

        public OutgameAvatar FindAvatar(int avatarId)
        {
            return OutgameDataLoader.FindAvatar(avatarId);
        }

        public OutgameSeasonEvent FindSeasonEvent(int eventId)
        {
            return OutgameDataLoader.FindSeasonEvent(eventId);
        }
    }
}
