using System;
using ProjectLink.Contracts.Account;
using ProjectLink.Contracts.Bootstrap;
using ProjectLink.Contracts.Daily;
using ProjectLink.Contracts.Event;
using ProjectLink.Contracts.Item;
using ProjectLink.Contracts.Lobby;
using ProjectLink.Contracts.Progress;
using ProjectLink.Contracts.Ranking;
using ProjectLink.Contracts.Reward;
using ProjectLink.Contracts.Settings;
using ProjectLink.Contracts.Shop;
using ProjectLink.Contracts.Stage;
using ProjectLink.Contracts.Stamina;

namespace ProjectLink.Services
{
    public readonly struct ServiceResult<T>
    {
        public ServiceResult(T value)
        {
            IsSuccess = true;
            Value = value;
            ErrorCode = "";
            ErrorMessage = "";
        }

        public ServiceResult(string errorCode, string errorMessage)
        {
            IsSuccess = false;
            Value = default;
            ErrorCode = errorCode ?? "";
            ErrorMessage = errorMessage ?? "";
        }

        public bool IsSuccess { get; }
        public T Value { get; }
        public string ErrorCode { get; }
        public string ErrorMessage { get; }
    }

    public interface IUiDataService
    {
        void GetBootstrapConfig(Action<ServiceResult<BootstrapConfigResponse>> onComplete);
        void GetAccountMe(Action<ServiceResult<AccountMeResponse>> onComplete);
        void GetLobbyState(Action<ServiceResult<LobbyStateResponse>> onComplete);
        void GetProgress(Action<ServiceResult<ProgressResponse>> onComplete);
        void StartStage(int stageId, Action<ServiceResult<StageStartResponse>> onComplete);
        void EndStage(int stageId, string sessionToken, string result, long clientElapsedMs, int movesUsed, Action<ServiceResult<StageEndResponse>> onComplete);
        void GetStamina(Action<ServiceResult<StaminaResponse>> onComplete);
        void ClaimStaminaAdReward(string adToken, Action<ServiceResult<StaminaAdRewardResponse>> onComplete);
        void RefillStamina(Action<ServiceResult<StaminaRefillResponse>> onComplete);
        void GetInventory(Action<ServiceResult<InventoryResponse>> onComplete);
        void GetShopCatalog(Action<ServiceResult<ShopCatalogResponse>> onComplete);
        void PurchaseShopProduct(int productId, int quantity, string iapReceiptData, Action<ServiceResult<ShopPurchaseResponse>> onComplete);
        void GetRanking(string category, Action<ServiceResult<RankingListResponse>> onComplete);
        void GetDailyChallenge(Action<ServiceResult<DailyChallengeResponse>> onComplete);
        void GetSeasonEvents(Action<ServiceResult<ActiveEventsResponse>> onComplete);
        void GetPlayerSettings(Action<ServiceResult<PlayerSettingsResponse>> onComplete);
        void ClaimReward(string rewardSource, string rewardToken, int multiplier, Action<ServiceResult<RewardClaimResponse>> onComplete);
    }
}
