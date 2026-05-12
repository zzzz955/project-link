using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace ProjectLink.Core
{
    public class NetworkManager : MonoBehaviour
    {
        const int LogBodyMaxLength = 300;

        public static NetworkManager Instance { get; private set; }

        [SerializeField] AppEnvironment environment = AppEnvironment.Dev;
        [SerializeField] string clientVersion = "1.0.0";
        [SerializeField] string protocolVersion = "1";
        [SerializeField] string metaHash = "";
        [SerializeField] bool httpLogging = true;

        string _baseUrl;
        string _authBaseUrl;
        IAuthService _authService;

        public string BaseUrl { get => _baseUrl; set => _baseUrl = value; }
        public string AuthBaseUrl => _authBaseUrl;
        public AppEnvironment Environment => environment;

        public IAuthService AuthService
        {
            get => _authService;
            set => _authService = value;
        }

        public string AccessToken => _authService?.GetToken() ?? "";

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            ApplyEnvironment();
            _authService ??= new PlatformAuthService(this, _authBaseUrl, clientVersion, protocolVersion, environment, httpLogging);
        }

        public void SetAuthToken(string accessToken) => _authService?.SetToken(accessToken);

        public void ClearAuthToken() => _authService?.ClearToken();

        public bool HasStoredAuthSession => _authService?.HasStoredSession ?? false;

        public string AuthProvider => _authService?.Provider ?? "";

        public void EnsureGuestAuth(Action<bool, string> onComplete)
            => _authService.EnsureAuth(onComplete);

        public void LoginGuest(Action<bool, string> onComplete)
            => _authService.LoginGuest(onComplete);

        public void LoginGoogle(string idToken, string nonce, Action<bool, string> onComplete)
            => _authService.LoginGoogle(idToken, nonce, onComplete);

        public void RefreshAuth(Action<bool, string> onComplete)
            => _authService.Refresh(onComplete);

        public void Logout(Action<bool, string> onComplete)
            => _authService.Logout(onComplete);

        public void Get(string endpoint, Action<bool, string> onComplete)
            => StartCoroutine(SendGet(endpoint, onComplete));

        public void Post(string endpoint, string jsonBody, Action<bool, string> onComplete)
            => StartCoroutine(SendWithBody(endpoint, "POST", jsonBody, onComplete));

        public void Patch(string endpoint, string jsonBody, Action<bool, string> onComplete)
            => StartCoroutine(SendWithBody(endpoint, "PATCH", jsonBody, onComplete));

        IEnumerator SendGet(string endpoint, Action<bool, string> onComplete)
        {
            using var req = UnityWebRequest.Get(BuildUrl(endpoint));
            ApplyHeaders(req);
            yield return req.SendWebRequest();
            Complete(req, "GET", null, onComplete);
        }

        IEnumerator SendWithBody(string endpoint, string method, string jsonBody, Action<bool, string> onComplete)
        {
            using var req = new UnityWebRequest(BuildUrl(endpoint), method);
            var bodyBytes = Encoding.UTF8.GetBytes(string.IsNullOrEmpty(jsonBody) ? "{}" : jsonBody);
            req.uploadHandler = new UploadHandlerRaw(bodyBytes);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            ApplyHeaders(req);
            yield return req.SendWebRequest();
            Complete(req, method, jsonBody, onComplete);
        }

        string BuildUrl(string endpoint)
        {
            if (string.IsNullOrEmpty(endpoint)) return _baseUrl;
            if (endpoint.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return endpoint;
            return $"{_baseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";
        }

        void ApplyHeaders(UnityWebRequest req)
        {
            req.SetRequestHeader("X-Client-Version", clientVersion);
            req.SetRequestHeader("X-Protocol-Version", protocolVersion);

            if (!string.IsNullOrEmpty(metaHash))
                req.SetRequestHeader("X-Meta-Hash", metaHash);

            var token = _authService?.GetToken();
            if (!string.IsNullOrEmpty(token))
                req.SetRequestHeader("Authorization", $"Bearer {token}");
        }

        void Complete(UnityWebRequest req, string method, string requestBody, Action<bool, string> onComplete)
        {
            var responseBody = req.downloadHandler?.text ?? "";
            if (req.result == UnityWebRequest.Result.Success)
            {
                if (httpLogging)
                {
                    var sb = new StringBuilder($"[HTTP] {method} {req.url} → {req.responseCode}");
                    if (!string.IsNullOrEmpty(requestBody)) sb.Append($"\nreq: {Clip(requestBody)}");
                    if (!string.IsNullOrEmpty(responseBody)) sb.Append($"\nres: {Clip(responseBody)}");
                    Debug.Log(sb);
                }
                onComplete?.Invoke(true, responseBody);
                return;
            }

            if (req.responseCode == 401)
            {
                if (httpLogging) Debug.LogWarning($"[HTTP] {method} {req.url} → 401 SESSION_EXPIRED");
                _authService?.ClearToken();
                onComplete?.Invoke(false, "SESSION_EXPIRED");
                return;
            }

            var status = req.responseCode > 0 ? req.responseCode.ToString() : req.result.ToString();
            var error = string.IsNullOrEmpty(responseBody) ? req.error : responseBody;
            if (httpLogging)
            {
                var sb = new StringBuilder($"[HTTP] {method} {req.url} → {status}");
                if (!string.IsNullOrEmpty(requestBody)) sb.Append($"\nreq: {Clip(requestBody)}");
                if (!string.IsNullOrEmpty(error)) sb.Append($"\nerr: {Clip(error)}");
                if (req.responseCode >= 500 || req.responseCode == 0)
                    Debug.LogError(sb);
                else
                    Debug.LogWarning(sb);
            }
            onComplete?.Invoke(false, error);
        }

        void ApplyEnvironment()
        {
            _baseUrl = environment == AppEnvironment.Prod ? AppConfig.ProdGameServerUrl : AppConfig.DevGameServerUrl;
            _authBaseUrl = environment == AppEnvironment.Prod ? AppConfig.ProdPlatformAuthUrl : AppConfig.DevPlatformAuthUrl;
        }

        static string Clip(string s) =>
            s.Length <= LogBodyMaxLength ? s : s[..LogBodyMaxLength] + $"…({s.Length})";
    }
}
