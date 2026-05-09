using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using ProjectLink.Contracts.Account;
using ProjectLink.Contracts.Bootstrap;
using ProjectLink.Contracts.Common;
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
using ProjectLink.Core;
using UnityEngine;

namespace ProjectLink.Services
{
    public sealed class HttpUiDataService : MonoBehaviour, IUiDataService
    {
        const float CacheTtlSeconds = 20f;
        readonly Dictionary<string, CacheEntry> _cache = new();

        sealed class CacheEntry
        {
            public string Payload;
            public float ExpiresAt;
        }

        public void GetBootstrapConfig(Action<ServiceResult<BootstrapConfigResponse>> onComplete)
            => Get(UiDataRoutes.BootstrapConfig, onComplete, false);

        public void GetAccountMe(Action<ServiceResult<AccountMeResponse>> onComplete)
            => Get(UiDataRoutes.AccountMe, onComplete);

        public void GetLobbyState(Action<ServiceResult<LobbyStateResponse>> onComplete)
            => Get(UiDataRoutes.LobbyState, onComplete);

        public void GetProgress(Action<ServiceResult<ProgressResponse>> onComplete)
            => Get(UiDataRoutes.Progress, onComplete);

        public void StartStage(int stageId, Action<ServiceResult<StageStartResponse>> onComplete)
            => Post(UiDataRoutes.StageStart(stageId), new StageStartRequest(), onComplete, InvalidateStageCaches);

        public void EndStage(int stageId, string sessionToken, string result, long clientElapsedMs, int movesUsed, Action<ServiceResult<StageEndResponse>> onComplete)
        {
            Post(UiDataRoutes.StageEnd(stageId), new StageEndRequest
            {
                SessionToken = sessionToken,
                Result = result,
                ClientElapsedMs = clientElapsedMs,
                MovesUsed = movesUsed,
            }, onComplete, InvalidateStageCaches);
        }

        public void GetStamina(Action<ServiceResult<StaminaResponse>> onComplete)
            => Get(UiDataRoutes.Stamina, onComplete);

        public void ClaimStaminaAdReward(string adToken, Action<ServiceResult<StaminaAdRewardResponse>> onComplete)
        {
            Post(UiDataRoutes.StaminaAdReward, new StaminaAdRewardRequest
            {
                AdToken = adToken ?? "",
            }, onComplete, InvalidateLobbyCaches);
        }

        public void RefillStamina(Action<ServiceResult<StaminaRefillResponse>> onComplete)
            => Post(UiDataRoutes.StaminaRefill, new StaminaRefillRequest(), onComplete, InvalidateLobbyCaches);

        public void GetInventory(Action<ServiceResult<InventoryResponse>> onComplete)
            => Get(UiDataRoutes.Inventory, onComplete);

        public void GetShopCatalog(Action<ServiceResult<ShopCatalogResponse>> onComplete)
            => Get(UiDataRoutes.ShopCatalog, onComplete);

        public void PurchaseShopProduct(int productId, int quantity, string iapReceiptData, Action<ServiceResult<ShopPurchaseResponse>> onComplete)
        {
            Post(UiDataRoutes.ShopPurchase, new ShopPurchaseRequest
            {
                ProductId = productId,
                Quantity = Math.Max(1, quantity),
                IapReceiptData = string.IsNullOrEmpty(iapReceiptData) ? null : iapReceiptData,
            }, onComplete, InvalidateShopCaches);
        }

        public void GetRanking(string category, Action<ServiceResult<RankingListResponse>> onComplete)
            => Get(UiDataRoutes.Ranking(category), onComplete);

        public void GetDailyChallenge(Action<ServiceResult<DailyChallengeResponse>> onComplete)
            => Get(UiDataRoutes.DailyChallenge, onComplete);

        public void GetSeasonEvents(Action<ServiceResult<ActiveEventsResponse>> onComplete)
            => Get(UiDataRoutes.SeasonEvents, onComplete);

        public void GetPlayerSettings(Action<ServiceResult<PlayerSettingsResponse>> onComplete)
            => Get(UiDataRoutes.PlayerSettings, onComplete);

        public void ClaimReward(string rewardSource, string rewardToken, int multiplier, Action<ServiceResult<RewardClaimResponse>> onComplete)
        {
            Post(UiDataRoutes.RewardsClaim, new RewardClaimRequest
            {
                RewardSource = rewardSource,
                RewardToken = rewardToken,
                Multiplier = multiplier,
            }, onComplete);
        }

        void Get<T>(string endpoint, Action<ServiceResult<T>> onComplete, bool requiresAuth = true)
        {
            if (!TryGetNetwork(out var network, onComplete)) return;

            if (TryServeCache(endpoint, onComplete))
                return;

            void Send() => network.Get(endpoint, (ok, payload) => Complete(ok, payload, onComplete, endpoint));

            if (requiresAuth)
                network.EnsureGuestAuth((ok, error) =>
                {
                    if (ok) Send();
                    else onComplete?.Invoke(new ServiceResult<T>("AUTH_FAILED", error));
                });
            else
                Send();
        }

        void Post<T>(string endpoint, object body, Action<ServiceResult<T>> onComplete, Action onSuccess = null)
        {
            if (!TryGetNetwork(out var network, onComplete)) return;
            network.EnsureGuestAuth((authOk, authError) =>
            {
                if (!authOk)
                {
                    onComplete?.Invoke(new ServiceResult<T>("AUTH_FAILED", authError));
                    return;
                }

                network.Post(endpoint, JsonConvert.SerializeObject(body), (ok, payload) => Complete<T>(ok, payload, result =>
                {
                    if (result.IsSuccess)
                        onSuccess?.Invoke();
                    onComplete?.Invoke(result);
                }));
            });
        }

        static bool TryGetNetwork<T>(out NetworkManager network, Action<ServiceResult<T>> onComplete)
        {
            network = NetworkManager.Instance;
            if (network != null) return true;

            onComplete?.Invoke(new ServiceResult<T>("NETWORK_UNAVAILABLE", "NetworkManager is not initialized."));
            return false;
        }

        void Complete<T>(bool ok, string payload, Action<ServiceResult<T>> onComplete, string cacheKey = null)
        {
            if (!ok)
            {
                onComplete?.Invoke(ParseError<T>(payload));
                return;
            }

            try
            {
                var value = JsonConvert.DeserializeObject<T>(payload);
                if (!string.IsNullOrEmpty(cacheKey))
                {
                    _cache[cacheKey] = new CacheEntry
                    {
                        Payload = payload,
                        ExpiresAt = Time.realtimeSinceStartup + CacheTtlSeconds,
                    };
                }
                onComplete?.Invoke(new ServiceResult<T>(value));
            }
            catch (Exception ex)
            {
                onComplete?.Invoke(new ServiceResult<T>("DESERIALIZE_FAILED", ex.Message));
            }
        }

        static ServiceResult<T> ParseError<T>(string payload)
        {
            try
            {
                var error = JsonConvert.DeserializeObject<ErrorResponse>(payload);
                if (error != null && !string.IsNullOrEmpty(error.ErrorCode))
                    return new ServiceResult<T>(error.ErrorCode, payload);
            }
            catch
            {
                // Fall through to raw payload.
            }

            return new ServiceResult<T>("HTTP_FAILED", payload);
        }

        bool TryServeCache<T>(string endpoint, Action<ServiceResult<T>> onComplete)
        {
            if (!_cache.TryGetValue(endpoint, out var entry) || entry.ExpiresAt < Time.realtimeSinceStartup)
                return false;

            try
            {
                onComplete?.Invoke(new ServiceResult<T>(JsonConvert.DeserializeObject<T>(entry.Payload)));
                return true;
            }
            catch
            {
                _cache.Remove(endpoint);
                return false;
            }
        }

        void InvalidateLobbyCaches()
        {
            _cache.Remove(UiDataRoutes.LobbyState);
            _cache.Remove(UiDataRoutes.Stamina);
            _cache.Remove(UiDataRoutes.ShopCatalog);
        }

        void InvalidateShopCaches()
        {
            _cache.Remove(UiDataRoutes.ShopCatalog);
            _cache.Remove(UiDataRoutes.Inventory);
            _cache.Remove(UiDataRoutes.LobbyState);
        }

        void InvalidateStageCaches()
        {
            _cache.Remove(UiDataRoutes.LobbyState);
            _cache.Remove(UiDataRoutes.Progress);
            _cache.Remove(UiDataRoutes.DailyChallenge);
            _cache.Remove(UiDataRoutes.Stamina);
        }
    }
}
