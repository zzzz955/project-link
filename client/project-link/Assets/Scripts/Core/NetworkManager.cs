using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace ProjectLink.Core
{
    public class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance { get; private set; }

        [SerializeField] AppEnvironment environment = AppEnvironment.Dev;
        [SerializeField] string clientVersion = "1.0.0";
        [SerializeField] string protocolVersion = "1";
        [SerializeField] string metaHash = "";

        string _baseUrl;
        IAuthService _authService;

        public string BaseUrl { get => _baseUrl; set => _baseUrl = value; }

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
            _baseUrl = environment == AppEnvironment.Prod ? AppConfig.ProdGameServerUrl : AppConfig.DevGameServerUrl;
            _authService ??= new MockAuthService();
        }

        public void SetAuthToken(string accessToken) => _authService?.SetToken(accessToken);

        public void ClearAuthToken() => _authService?.ClearToken();

        public void EnsureGuestAuth(Action<bool, string> onComplete)
            => _authService.EnsureAuth(onComplete);

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
            Complete(req, onComplete);
        }

        IEnumerator SendWithBody(string endpoint, string method, string jsonBody, Action<bool, string> onComplete)
        {
            using var req = new UnityWebRequest(BuildUrl(endpoint), method);
            var bodyBytes = System.Text.Encoding.UTF8.GetBytes(string.IsNullOrEmpty(jsonBody) ? "{}" : jsonBody);
            req.uploadHandler = new UploadHandlerRaw(bodyBytes);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            ApplyHeaders(req);
            yield return req.SendWebRequest();
            Complete(req, onComplete);
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

        void Complete(UnityWebRequest req, Action<bool, string> onComplete)
        {
            var body = req.downloadHandler?.text ?? "";
            if (req.result == UnityWebRequest.Result.Success)
            {
                onComplete?.Invoke(true, body);
                return;
            }

            if (req.responseCode == 401)
                _authService?.ClearToken();

            onComplete?.Invoke(false, string.IsNullOrEmpty(body) ? req.error : body);
        }
    }
}
