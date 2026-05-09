using System.Collections.Generic;
using ProjectLink.Data;
using ProjectLink.Data.Generated;

namespace ProjectLink.Services
{
    public interface IStaticCatalogService
    {
        IReadOnlyList<OutgameShopCatalog> GetEnabledShopProducts(string category = null);
        IReadOnlyList<OutgameDailyReward> DailyRewards { get; }
        IReadOnlyList<OutgameSeasonEvent> SeasonEvents { get; }
        OutgameStaminaConfig StaminaConfig { get; }
        OutgameDailyChallenge DailyChallengeConfig { get; }
        IngameItem FindItem(int itemId);
        OutgameAvatar FindAvatar(int avatarId);
        OutgameSeasonEvent FindSeasonEvent(int eventId);
    }

    public sealed class StaticCatalogService : IStaticCatalogService
    {
        public IReadOnlyList<OutgameDailyReward> DailyRewards => OutgameDataLoader.DailyRewards;
        public IReadOnlyList<OutgameSeasonEvent> SeasonEvents => OutgameDataLoader.SeasonEvents;
        public OutgameStaminaConfig StaminaConfig => OutgameDataLoader.StaminaConfig;
        public OutgameDailyChallenge DailyChallengeConfig => OutgameDataLoader.DailyChallengeConfig;

        public IReadOnlyList<OutgameShopCatalog> GetEnabledShopProducts(string category = null)
        {
            return OutgameDataLoader.GetEnabledShopProducts(category);
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
