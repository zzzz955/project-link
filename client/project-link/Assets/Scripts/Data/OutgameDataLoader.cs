using System;
using System.Collections.Generic;
using ProjectLink.Data.Generated;
using ProjectLink.Utils;

namespace ProjectLink.Data
{
    public static class OutgameDataLoader
    {
        static OutgameAvatar[] _avatars;
        static OutgameSeasonEvent[] _seasonEvents;
        static OutgameShopCatalog[] _shopCatalog;
        static OutgameStaminaConfig[] _staminaConfigs;
        static OutgameDailyReward[] _dailyRewards;
        static IngameItem[] _items;

        public static IReadOnlyList<OutgameAvatar> Avatars
        {
            get { EnsureLoaded(); return _avatars; }
        }

        public static IReadOnlyList<OutgameSeasonEvent> SeasonEvents
        {
            get { EnsureLoaded(); return _seasonEvents; }
        }

        public static OutgameStaminaConfig StaminaConfig
        {
            get
            {
                EnsureLoaded();
                return _staminaConfigs.Length > 0 ? _staminaConfigs[0] : null;
            }
        }

        public static IReadOnlyList<OutgameDailyReward> DailyRewards
        {
            get { EnsureLoaded(); return _dailyRewards; }
        }

        public static IReadOnlyList<OutgameShopCatalog> GetEnabledShopProducts(string category = null)
        {
            EnsureLoaded();
            var result = new List<OutgameShopCatalog>();

            foreach (var product in _shopCatalog)
            {
                if (!product.isEnabled) continue;
                if (!string.IsNullOrEmpty(category) && !string.Equals(product.category, category, StringComparison.OrdinalIgnoreCase))
                    continue;

                result.Add(product);
            }

            result.Sort((a, b) => a.sortOrder.CompareTo(b.sortOrder));
            return result;
        }

        public static IReadOnlyList<IngameItem> GetAllItems()
        {
            EnsureLoaded();
            return _items;
        }

        public static IngameItem FindItem(int itemId)
        {
            EnsureLoaded();
            return Array.Find(_items, item => item.id == itemId);
        }

        public static OutgameAvatar FindAvatar(int avatarId)
        {
            EnsureLoaded();
            return Array.Find(_avatars, avatar => avatar.id == avatarId);
        }

        public static OutgameSeasonEvent FindSeasonEvent(int eventId)
        {
            EnsureLoaded();
            return Array.Find(_seasonEvents, seasonEvent => seasonEvent.eventId == eventId);
        }

        static void EnsureLoaded()
        {
            if (_shopCatalog != null) return;

            _avatars = CsvLoader.Load<OutgameAvatar>(OutgameAvatar.ResourcePath);
            _seasonEvents = CsvLoader.Load<OutgameSeasonEvent>(OutgameSeasonEvent.ResourcePath);
            _shopCatalog = CsvLoader.Load<OutgameShopCatalog>(OutgameShopCatalog.ResourcePath);
            _staminaConfigs = CsvLoader.Load<OutgameStaminaConfig>(OutgameStaminaConfig.ResourcePath);
            _dailyRewards = CsvLoader.Load<OutgameDailyReward>(OutgameDailyReward.ResourcePath);
            _items = CsvLoader.Load<IngameItem>(IngameItem.ResourcePath);
        }
    }
}
