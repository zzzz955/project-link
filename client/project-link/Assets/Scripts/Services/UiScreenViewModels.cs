using System;
using ProjectLink.Contracts.Bootstrap;
using ProjectLink.Contracts.Ranking;
using ProjectLink.Core;
using ProjectLink.Utils;
using UnityEngine;

namespace ProjectLink.Services
{
    public abstract class UiViewModelBase
    {
        protected UiViewModelBase(string scope)
        {
            Scope = scope ?? "";
        }

        public string Scope { get; }
        public bool IsLoading { get; private set; }
        public string ErrorCode { get; private set; } = "";
        public string ErrorMessage { get; private set; } = "";

        public event Action Changed;

        protected void SetLoading(bool value)
        {
            if (IsLoading == value) return;
            IsLoading = value;
            UiEventBus.Publish(new UiBusyChanged(Scope, value));
            NotifyChanged();
        }

        protected void ClearError()
        {
            ErrorCode = "";
            ErrorMessage = "";
        }

        protected void SetError(string errorCode, string errorMessage, bool blocking = false)
        {
            ErrorCode = errorCode ?? "";
            ErrorMessage = errorMessage ?? "";
            UiEventBus.Publish(new UiErrorRaised(Scope, ErrorCode, ErrorMessage, blocking));
            NotifyChanged();
        }

        protected void NotifyChanged()
        {
            Changed?.Invoke();
            UiEventBus.Publish(new UiViewModelChanged(Scope, this));
        }
    }

    public sealed class BootstrapViewModel : UiViewModelBase
    {
        readonly IUiDataService _uiData;

        public BootstrapViewModel(IUiDataService uiData) : base("bootstrap")
        {
            _uiData = uiData;
        }

        public float Progress { get; private set; }
        public string StatusStringId { get; private set; } = "bootstrap.loading";
        public string Version { get; private set; } = "";
        public string MaintenanceMessage { get; private set; } = "";
        public bool RetryVisible { get; private set; }
        public bool ReadyToEnterTitle { get; private set; }
        public bool RequiresForceUpdate { get; private set; }

        public void Load()
        {
            ReadyToEnterTitle = false;
            RequiresForceUpdate = false;
            RetryVisible = false;
            Progress = 0.35f;
            StatusStringId = "bootstrap.loading";
            ClearError();
            SetLoading(true);

            _uiData.GetBootstrapConfig(result =>
            {
                SetLoading(false);
                if (!result.IsSuccess)
                {
                    RetryVisible = true;
                    StatusStringId = "bootstrap.network_error";
                    SetError(result.ErrorCode, result.ErrorMessage);
                    return;
                }

                Apply(result.Value);
            });
        }

        void Apply(BootstrapConfigResponse config)
        {
            Version = config?.ClientVersion ?? "";
            MaintenanceMessage = config?.MaintenanceMessage ?? "";

            if (IsVersionGreater(config?.RequiredClientVersion, config?.ClientVersion))
            {
                RequiresForceUpdate = true;
                Progress = 1f;
                StatusStringId = "popup.force_update.title";
                CsvLoader.ClearPatch();
                NotifyChanged();
                return;
            }

            // DataSchemaVersion mismatch means client code change is needed — force update
            var embeddedSchema = LoadEmbeddedText("data/data_schema_version");
            if (!string.IsNullOrEmpty(config?.DataSchemaVersion)
                && !string.IsNullOrEmpty(embeddedSchema)
                && config.DataSchemaVersion != embeddedSchema)
            {
                RequiresForceUpdate = true;
                Progress = 1f;
                StatusStringId = "popup.force_update.title";
                CsvLoader.ClearPatch();
                NotifyChanged();
                return;
            }

            // MetaHash mismatch — data values changed, patch without binary update
            var localMetaHash = CsvLoader.GetPatchedMetaHash();
            if (string.IsNullOrEmpty(localMetaHash))
                localMetaHash = LoadEmbeddedText("data/meta_hash_cs");

            if (!string.IsNullOrEmpty(config?.MetaHash)
                && !string.IsNullOrEmpty(localMetaHash)
                && config.MetaHash != localMetaHash)
            {
                Progress = 0.6f;
                StatusStringId = "bootstrap.patching";
                NotifyChanged();

                _uiData.GetDataBundle(bundleResult =>
                {
                    if (bundleResult.IsSuccess)
                        ApplyPatch(bundleResult.Value);
                    else
                        Debug.LogWarning($"[Bootstrap] Patch download failed: {bundleResult.ErrorCode}. Proceeding with bundled data.");

                    Progress = 1f;
                    StatusStringId = "bootstrap.ready";
                    ReadyToEnterTitle = true;
                    NotifyChanged();
                });
                return;
            }

            Progress = 1f;
            StatusStringId = "bootstrap.ready";
            ReadyToEnterTitle = true;
            NotifyChanged();
        }

        static void ApplyPatch(DataBundleResponse bundle)
        {
            foreach (var kv in bundle.Files)
                CsvLoader.WritePatchFile(kv.Key, kv.Value);
            CsvLoader.SavePatchedMetaHash(bundle.MetaHash);
        }

        static string LoadEmbeddedText(string resourcePath)
        {
            var asset = UnityEngine.Resources.Load<UnityEngine.TextAsset>(resourcePath);
            return asset != null ? asset.text.Trim() : "";
        }

        static bool IsVersionGreater(string required, string current)
        {
            if (string.IsNullOrWhiteSpace(required) || string.IsNullOrWhiteSpace(current))
                return false;

            return System.Version.TryParse(required, out var req)
                && System.Version.TryParse(current, out var cur)
                && req > cur;
        }
    }

    public sealed class TitleViewModel : UiViewModelBase
    {
        readonly IUiDataService _uiData;
        readonly NetworkManager _network;

        BootstrapConfigResponse _bootstrap;

        public TitleViewModel(IUiDataService uiData, NetworkManager network) : base("title")
        {
            _uiData = uiData;
            _network = network;
        }

        public string Version { get; private set; } = "";
        public bool TitleControlsVisible { get; private set; } = true;
        public bool IsAuthenticated { get; private set; }
        public bool RequiresForceUpdate { get; private set; }
        public bool IsMaintenance { get; private set; }
        public string MaintenanceMessage { get; private set; } = "";
        public bool EnterLobbyRequested { get; private set; }

        public void Load()
        {
            EnterLobbyRequested = false;
            ClearError();
            bool suppressSilentLogin = GameContext.ConsumeTitleSilentLoginSuppression();
            SetLoading(true);

            _uiData.GetBootstrapConfig(result =>
            {
                if (!result.IsSuccess)
                {
                    TitleControlsVisible = true;
                    SetLoading(false);
                    SetError(result.ErrorCode, result.ErrorMessage);
                    return;
                }

                _bootstrap = result.Value;
                Version = _bootstrap.ClientVersion;
                RequiresForceUpdate = IsVersionGreater(_bootstrap.RequiredClientVersion, _bootstrap.ClientVersion);
                IsMaintenance = _bootstrap.Maintenance;
                MaintenanceMessage = _bootstrap.MaintenanceMessage ?? "";

                if (RequiresForceUpdate || suppressSilentLogin || _network == null || !_network.HasStoredAuthSession)
                {
                    TitleControlsVisible = true;
                    SetLoading(false);
                    NotifyChanged();
                    return;
                }

                TitleControlsVisible = false;
                _network.RefreshAuth(CompleteAuth);
            });
        }

        public void TapToStart()
        {
            EnterLobbyRequested = false;
            if (_network == null)
            {
                SetError("NETWORK_UNAVAILABLE", "NetworkManager is not initialized.");
                return;
            }

            SetLoading(true);
            if (_network.HasStoredAuthSession)
                _network.RefreshAuth(CompleteTapRefresh);
            else
                _network.LoginGuest(CompleteAuth);
        }

        public void LoginGoogle(string idToken, string nonce = "")
        {
            if (_network == null)
            {
                SetError("NETWORK_UNAVAILABLE", "NetworkManager is not initialized.");
                return;
            }

            SetLoading(true);
            _network.LoginGoogle(idToken, nonce, CompleteAuth);
        }

        public void LoginApple()
        {
            SetError("PROVIDER_UNAVAILABLE", "Apple auth is not exposed by platform auth yet.");
        }

        void CompleteAuth(bool ok, string error)
        {
            SetLoading(false);
            if (!ok)
            {
                IsAuthenticated = false;
                TitleControlsVisible = true;
                SetError(string.IsNullOrEmpty(error) ? "AUTH_FAILED" : error, error);
                return;
            }

            IsAuthenticated = true;
            TitleControlsVisible = false;
            EnterLobbyRequested = !IsMaintenance && !RequiresForceUpdate;
            NotifyChanged();
        }

        void CompleteTapRefresh(bool ok, string error)
        {
            if (!ok && error == "SESSION_EXPIRED")
            {
                _network.LoginGuest(CompleteAuth);
                return;
            }

            CompleteAuth(ok, error);
        }

        static bool IsVersionGreater(string required, string current)
        {
            if (string.IsNullOrWhiteSpace(required) || string.IsNullOrWhiteSpace(current))
                return false;

            return System.Version.TryParse(required, out var req)
                && System.Version.TryParse(current, out var cur)
                && req > cur;
        }
    }

    public sealed class LobbyViewModel : UiViewModelBase
    {
        readonly IUiDataService _uiData;
        readonly IStaticCatalogService _catalog;

        public LobbyViewModel(IUiDataService uiData, IStaticCatalogService catalog) : base("lobby")
        {
            _uiData = uiData;
            _catalog = catalog;
        }

        public LobbyScreenModel Lobby { get; private set; }
        public ShopScreenModel Shop { get; private set; }
        public RankingListResponse Ranking { get; private set; }
        public string RankingCategory { get; private set; } = "global_stages";

        public void LoadLobby(Action onDone = null)
        {
            SetLoading(true);
            _uiData.GetLobbyState(result =>
            {
                SetLoading(false);
                if (!result.IsSuccess)
                {
                    SetError(result.ErrorCode, result.ErrorMessage);
                    onDone?.Invoke();
                    return;
                }

                Lobby = UiViewModelMapper.ToLobbyScreen(result.Value, _catalog);
                ClearError();
                NotifyChanged();
                onDone?.Invoke();
            });
        }

        public void LoadShop(Action onDone = null)
        {
            SetLoading(true);
            _uiData.GetShopCatalog(result =>
            {
                SetLoading(false);
                if (!result.IsSuccess)
                {
                    SetError(result.ErrorCode, result.ErrorMessage);
                    onDone?.Invoke();
                    return;
                }

                Shop = UiViewModelMapper.ToShopScreen(result.Value, _catalog);
                ClearError();
                NotifyChanged();
                onDone?.Invoke();
            });
        }

        public void LoadRanking(string category, Action onDone = null)
        {
            RankingCategory = string.IsNullOrEmpty(category) ? "global_stages" : category;
            SetLoading(true);
            _uiData.GetRanking(RankingCategory, result =>
            {
                SetLoading(false);
                if (!result.IsSuccess)
                {
                    SetError(result.ErrorCode, result.ErrorMessage);
                    onDone?.Invoke();
                    return;
                }

                Ranking = result.Value;
                ClearError();
                NotifyChanged();
                onDone?.Invoke();
            });
        }
    }
}
