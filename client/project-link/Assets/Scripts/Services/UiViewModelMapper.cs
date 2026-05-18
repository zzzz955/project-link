using ProjectLink.Contracts.Lobby;
using ProjectLink.Contracts.Shop;
using ProjectLink.Contracts.Stamina;

namespace ProjectLink.Services
{
    public static class UiViewModelMapper
    {
        public static LobbyScreenModel ToLobbyScreen(LobbyStateResponse response, IStaticCatalogService staticCatalog)
        {
            if (response == null) return null;

            var avatar = staticCatalog?.FindAvatar(response.Profile.AvatarId);
            var seasonEvent = response.SeasonEvent != null ? staticCatalog?.FindSeasonEvent(response.SeasonEvent.EventId) : null;

            return new LobbyScreenModel
            {
                DisplayName = response.Profile.DisplayName,
                AvatarId = response.Profile.AvatarId,
                AvatarIconPath = avatar?.iconPath,
                StaminaCurrent = response.Stamina.Current,
                StaminaMax = response.Stamina.Max,
                NextRechargeAt = response.Stamina.NextRechargeAt,
                SoftCurrency = response.Currency.SoftAmount,
                HighestStageId = response.ProgressSummary.HighestStageId,
                NextUnlockedStageId = response.ProgressSummary.NextUnlockedStageId,
                TotalStarsEarned = response.ProgressSummary.TotalStarsEarned,
                CanPlay = response.Stamina.Current > 0,
                StreakChallenge = new StreakChallengeModel
                {
                    EventStatus          = response.StreakChallenge.EventStatus,
                    RemainingTimeIso     = response.StreakChallenge.RemainingTimeIso,
                    CurrentLevel         = response.StreakChallenge.CurrentLevel,
                    CurrentLevelCount    = response.StreakChallenge.CurrentLevelCount,
                    CurrentLevelRequired = response.StreakChallenge.CurrentLevelRequired,
                    HasPendingReward     = response.StreakChallenge.HasPendingReward,
                },
                SeasonEvent = response.SeasonEvent == null ? null : new SeasonEventModel
                {
                    EventId = response.SeasonEvent.EventId,
                    Name = string.IsNullOrEmpty(response.SeasonEvent.Name) ? seasonEvent?.name : response.SeasonEvent.Name,
                    EndAt = response.SeasonEvent.EndAt,
                    IsActive = response.SeasonEvent.IsActive,
                    MetricLabel = seasonEvent?.metricLabel,
                },
            };
        }

        public static ShopScreenModel ToShopScreen(ShopCatalogResponse response, IStaticCatalogService staticCatalog)
        {
            if (response == null) return null;

            var model = new ShopScreenModel { SoftBalance = response.SoftBalance };

            foreach (var product in response.Products)
            {
                var item = product.GrantItemId > 0 ? staticCatalog?.FindItem(product.GrantItemId) : null;
                model.Products.Add(new ShopProductModel
                {
                    ProductId = product.ProductId,
                    Category = product.Category,
                    Name = product.Name,
                    GrantItemId = product.GrantItemId,
                    GrantQuantity = product.GrantQuantity,
                    PriceSoft = product.PriceSoft,
                    PriceIapSku = product.PriceIapSku,
                    SortOrder = product.SortOrder,
                    ItemName = item?.name,
                    ItemDescription = item?.description_key,
                });
            }

            model.Products.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
            return model;
        }

        public static EnergyPopupModel ToEnergyPopup(StaminaResponse response, IStaticCatalogService staticCatalog)
        {
            if (response == null) return null;

            return new EnergyPopupModel
            {
                Current = response.Current,
                Max = response.Max,
                NextRechargeAt = response.NextRechargeAt,
                AdRewardAmount = staticCatalog?.StaminaConfig?.adRewardAmount ?? 0,
            };
        }
    }
}
