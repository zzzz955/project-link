using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ProjectLink.Contracts.Account;
using ProjectLink.Contracts.Bootstrap;
using ProjectLink.Contracts.Common;
using ProjectLink.Contracts.StreakChallenge;
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
        static readonly JsonSerializerSettings JsonSettings = new()
        {
            DateParseHandling = DateParseHandling.None,
            ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver(),
        };

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

        public void GetStreakChallengeState(Action<ServiceResult<StreakChallengeStateResponse>> onComplete)
        {
            _cache.Remove(UiDataRoutes.StreakChallenge);
            Get(UiDataRoutes.StreakChallenge, onComplete);
        }

        public void ActivateStreakChallenge(Action<ServiceResult<StreakChallengeStateResponse>> onComplete)
            => Post(UiDataRoutes.StreakChallengeActivate, new StreakChallengeActivateRequest(), onComplete);

        public void StartStreakLevel(int level, Action<ServiceResult<StreakChallengeStateResponse>> onComplete)
            => Post(UiDataRoutes.StreakChallengeStartLevel(level), new StreakChallengeStartLevelRequest(), onComplete);

        public void ClaimStreakReward(int level, string correlationId, Action<ServiceResult<StreakChallengeClaimRewardResponse>> onComplete)
            => Post(UiDataRoutes.StreakChallengeClaimReward(level), new StreakChallengeClaimRewardRequest { CorrelationId = correlationId }, onComplete, InvalidateLobbyCaches);

        public void GetSeasonEvents(Action<ServiceResult<ActiveEventsResponse>> onComplete)
            => Get(UiDataRoutes.SeasonEvents, onComplete);

        public void GetPlayerSettings(Action<ServiceResult<PlayerSettingsResponse>> onComplete)
            => Get(UiDataRoutes.PlayerSettings, onComplete);

        public void UpdatePlayerSettings(PlayerSettingsUpdateRequest request, Action<ServiceResult<PlayerSettingsResponse>> onComplete)
        {
            _cache.Remove(UiDataRoutes.PlayerSettings);
            Patch(UiDataRoutes.PlayerSettings, request, onComplete);
        }

        public void ExtendStageTime(int stageId, Action<ServiceResult<StageExtendResponse>> onComplete)
            => Post(UiDataRoutes.StageExtend(stageId), new StageEndRequest { SessionToken = GameContext.StageSessionToken }, onComplete);

        public void ClaimReward(string rewardSource, string rewardToken, int multiplier, Action<ServiceResult<RewardClaimResponse>> onComplete)
        {
            Post(UiDataRoutes.RewardsClaim, new RewardClaimRequest
            {
                RewardSource = rewardSource,
                RewardToken = rewardToken,
                Multiplier = multiplier,
            }, onComplete);
        }

        public void UseIngameItem(int itemId, string sessionToken, Action<ServiceResult<InGameItemUseResponse>> onComplete)
            => Post(UiDataRoutes.UseIngameItem, new InGameItemUseRequest { ItemId = itemId, StageSessionToken = sessionToken }, onComplete);

        public void PurchaseItem(int itemId, int quantity, Action<ServiceResult<ItemPurchaseResponse>> onComplete)
            => Post(UiDataRoutes.ItemPurchase, new ItemPurchaseRequest
            {
                ItemId = itemId,
                Quantity = Math.Max(1, quantity),
            }, onComplete, InvalidateInventoryCaches);

        void Get<T>(string endpoint, Action<ServiceResult<T>> onComplete, bool requiresAuth = true)
        {
            if (!TryGetNetwork(out var network, onComplete)) return;

            if (TryServeCache(endpoint, onComplete))
                return;

            void Send()
            {
                UiEventBus.Publish(new UiBusyChanged(endpoint, true));
                network.Get(endpoint, (ok, payload) =>
                {
                    UiEventBus.Publish(new UiBusyChanged(endpoint, false));
                    Complete(ok, payload, onComplete, endpoint);
                });
            }

            if (requiresAuth)
                network.EnsureGuestAuth((ok, error) =>
                {
                    if (ok) { Send(); return; }
                    if (error == "SESSION_EXPIRED")
                        PopupManager.Request(PopupId.SessionExpired);
                    onComplete?.Invoke(new ServiceResult<T>(error, error));
                });
            else
                Send();
        }

        void Post<T>(string endpoint, object body, Action<ServiceResult<T>> onComplete, Action onSuccess = null)
        {
            if (!TryGetNetwork(out var network, onComplete)) return;
            UiEventBus.Publish(new UiBusyChanged(endpoint, true));
            network.EnsureGuestAuth((authOk, authError) =>
            {
                if (!authOk)
                {
                    UiEventBus.Publish(new UiBusyChanged(endpoint, false));
                    if (authError == "SESSION_EXPIRED")
                        PopupManager.Request(PopupId.SessionExpired);
                    onComplete?.Invoke(new ServiceResult<T>(authError, authError));
                    return;
                }

                network.Post(endpoint, JsonConvert.SerializeObject(body, JsonSettings), (ok, payload) =>
                {
                    UiEventBus.Publish(new UiBusyChanged(endpoint, false));
                    Complete<T>(ok, payload, result =>
                    {
                        if (result.IsSuccess)
                            onSuccess?.Invoke();
                        onComplete?.Invoke(result);
                    });
                });
            });
        }

        void Patch<T>(string endpoint, object body, Action<ServiceResult<T>> onComplete, Action onSuccess = null)
        {
            if (!TryGetNetwork(out var network, onComplete)) return;
            UiEventBus.Publish(new UiBusyChanged(endpoint, true));
            network.EnsureGuestAuth((authOk, authError) =>
            {
                if (!authOk)
                {
                    UiEventBus.Publish(new UiBusyChanged(endpoint, false));
                    if (authError == "SESSION_EXPIRED")
                        PopupManager.Request(PopupId.SessionExpired);
                    onComplete?.Invoke(new ServiceResult<T>(authError, authError));
                    return;
                }

                network.Patch(endpoint, JsonConvert.SerializeObject(body, JsonSettings), (ok, payload) =>
                {
                    UiEventBus.Publish(new UiBusyChanged(endpoint, false));
                    Complete<T>(ok, payload, result =>
                    {
                        if (result.IsSuccess)
                            onSuccess?.Invoke();
                        onComplete?.Invoke(result);
                    });
                });
            });
        }

        static bool TryGetNetwork<T>(out NetworkManager network, Action<ServiceResult<T>> onComplete)
        {
            network = NetworkManager.Instance;
            if (network != null) return true;

            onComplete?.Invoke(new ServiceResult<T>("NETWORK_UNAVAILABLE", "NetworkManager is not initialized."));
            return false;
        }

        bool _sessionExpiredPopupRequested;

        void Complete<T>(bool ok, string payload, Action<ServiceResult<T>> onComplete, string cacheKey = null)
        {
            if (!ok)
            {
                var result = ParseError<T>(payload);
                if (result.ErrorCode == "SESSION_EXPIRED" && !_sessionExpiredPopupRequested)
                {
                    _sessionExpiredPopupRequested = true;
                    PopupManager.Request(PopupId.SessionExpired);
                }
                UiEventBus.Publish(new UiErrorRaised(cacheKey ?? "", result.ErrorCode, result.ErrorMessage));
                onComplete?.Invoke(result);
                return;
            }

            T value;
            try
            {
                value = JsonConvert.DeserializeObject<T>(payload, JsonSettings);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"UI data deserialize failed for {cacheKey ?? "<uncached>"}: {ex.Message}\n{BuildDeserializeDiagnostics<T>(payload, ex)}\npayload: {payload}");
                var result = new ServiceResult<T>("DESERIALIZE_FAILED", ex.Message);
                UiEventBus.Publish(new UiErrorRaised(cacheKey ?? "", result.ErrorCode, result.ErrorMessage));
                onComplete?.Invoke(result);
                return;
            }

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

        static string BuildDeserializeDiagnostics<T>(string payload, Exception ex)
        {
            var lines = new List<string>
            {
                $"target: {typeof(T).FullName}",
                $"target assembly: {typeof(T).Assembly.FullName}",
                $"exception: {ex.GetType().FullName}",
            };

            if (ex is JsonSerializationException jsonEx && !string.IsNullOrEmpty(jsonEx.Path))
                lines.Add($"json path: {jsonEx.Path}");

            var suspiciousPaths = FindObjectToStringCandidates(typeof(T), payload);
            if (suspiciousPaths.Count > 0)
                lines.Add($"object->string candidates: {string.Join(", ", suspiciousPaths)}");

            lines.Add(ex.ToString());
            return string.Join("\n", lines);
        }

        static List<string> FindObjectToStringCandidates(Type targetType, string payload)
        {
            var results = new List<string>();
            try
            {
                WalkExpectedJson(targetType, JToken.Parse(payload), "$", results);
            }
            catch (Exception scanEx)
            {
                results.Add($"<scan failed: {scanEx.Message}>");
            }
            return results;
        }

        static void WalkExpectedJson(Type expectedType, JToken token, string path, List<string> results)
        {
            expectedType = Nullable.GetUnderlyingType(expectedType) ?? expectedType;
            if (expectedType == typeof(string))
            {
                if (token.Type == JTokenType.Object || token.Type == JTokenType.Array)
                    results.Add(path);
                return;
            }

            if (token is JArray array)
            {
                var elementType = GetEnumerableElementType(expectedType);
                if (elementType == null) return;
                for (var i = 0; i < array.Count; i++)
                    WalkExpectedJson(elementType, array[i], $"{path}[{i}]", results);
                return;
            }

            if (token is not JObject obj || expectedType == typeof(object))
                return;

            foreach (var prop in expectedType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (!prop.CanWrite)
                    continue;

                var jsonName = char.ToLowerInvariant(prop.Name[0]) + prop.Name.Substring(1);
                if (obj.TryGetValue(jsonName, StringComparison.OrdinalIgnoreCase, out var child))
                    WalkExpectedJson(prop.PropertyType, child, $"{path}.{jsonName}", results);
            }
        }

        static Type GetEnumerableElementType(Type type)
        {
            if (type.IsArray)
                return type.GetElementType();

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                return type.GetGenericArguments()[0];

            return null;
        }

        static ServiceResult<T> ParseError<T>(string payload)
        {
            if (payload == "SESSION_EXPIRED")
                return new ServiceResult<T>("SESSION_EXPIRED", payload);

            try
            {
                var error = JsonConvert.DeserializeObject<ErrorResponse>(payload, JsonSettings);
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

            T value;
            try
            {
                value = JsonConvert.DeserializeObject<T>(entry.Payload, JsonSettings);
            }
            catch
            {
                _cache.Remove(endpoint);
                return false;
            }

            onComplete?.Invoke(new ServiceResult<T>(value));
            return true;
        }

        void InvalidateInventoryCaches()
        {
            _cache.Remove(UiDataRoutes.Inventory);
            _cache.Remove(UiDataRoutes.LobbyState);
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
            _cache.Remove(UiDataRoutes.StreakChallenge);
            _cache.Remove(UiDataRoutes.Stamina);
        }
    }
}
