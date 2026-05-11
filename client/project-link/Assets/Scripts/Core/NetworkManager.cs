using System;
using System.Collections;
using Newtonsoft.Json;
using ProjectLink.Contracts.Account;
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
        [SerializeField] bool autoGuestLogin = true;
        [SerializeField] string guestLoginEndpoint = "/api/auth/guest";

        string baseUrl;

        public string BaseUrl
        {
            get => baseUrl;
            set => baseUrl = value;
        }

        public string AccessToken { get; private set; }
        bool _authInFlight;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            baseUrl = environment == AppEnvironment.Prod ? AppConfig.ProdGameServerUrl : AppConfig.DevGameServerUrl;
        }

        public void SetAuthToken(string accessToken)
        {
            AccessToken = accessToken;
        }

        public void ClearAuthToken()
        {
            AccessToken = "";
        }

        public void EnsureGuestAuth(Action<bool, string> onComplete)
        {
            if (!autoGuestLogin || !string.IsNullOrEmpty(AccessToken))
            {
                onComplete?.Invoke(true, "");
                return;
            }

            if (_authInFlight)
            {
                StartCoroutine(WaitForAuth(onComplete));
                return;
            }

            StartCoroutine(SendGuestLogin(onComplete));
        }

        public void Get(string endpoint, Action<bool, string> onComplete)
        {
            StartCoroutine(SendGet(endpoint, onComplete));
        }

        public void Post(string endpoint, string jsonBody, Action<bool, string> onComplete)
        {
            StartCoroutine(SendWithBody(endpoint, "POST", jsonBody, onComplete));
        }

        public void Patch(string endpoint, string jsonBody, Action<bool, string> onComplete)
        {
            StartCoroutine(SendWithBody(endpoint, "PATCH", jsonBody, onComplete));
        }

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

        IEnumerator SendGuestLogin(Action<bool, string> onComplete)
        {
            _authInFlight = true;
            using var req = new UnityWebRequest(BuildUrl(guestLoginEndpoint), "POST");
            var bodyBytes = System.Text.Encoding.UTF8.GetBytes("{}");
            req.uploadHandler = new UploadHandlerRaw(bodyBytes);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("X-Client-Version", clientVersion);
            req.SetRequestHeader("X-Protocol-Version", protocolVersion);
            if (!string.IsNullOrEmpty(metaHash))
                req.SetRequestHeader("X-Meta-Hash", metaHash);

            yield return req.SendWebRequest();

            var body = req.downloadHandler?.text ?? "";
            if (req.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var response = JsonConvert.DeserializeObject<AuthResponse>(body);
                    AccessToken = response?.AccessToken ?? "";
                    _authInFlight = false;
                    onComplete?.Invoke(!string.IsNullOrEmpty(AccessToken), string.IsNullOrEmpty(AccessToken) ? "AUTH_TOKEN_EMPTY" : "");
                }
                catch (Exception ex)
                {
                    _authInFlight = false;
                    onComplete?.Invoke(false, ex.Message);
                }
            }
            else
            {
                _authInFlight = false;
                onComplete?.Invoke(false, string.IsNullOrEmpty(body) ? req.error : body);
            }
        }

        IEnumerator WaitForAuth(Action<bool, string> onComplete)
        {
            while (_authInFlight)
                yield return null;

            onComplete?.Invoke(!string.IsNullOrEmpty(AccessToken), string.IsNullOrEmpty(AccessToken) ? "AUTH_TOKEN_EMPTY" : "");
        }

        string BuildUrl(string endpoint)
        {
            if (string.IsNullOrEmpty(endpoint)) return baseUrl;
            if (endpoint.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return endpoint;
            return $"{baseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";
        }

        void ApplyHeaders(UnityWebRequest req)
        {
            req.SetRequestHeader("X-Client-Version", clientVersion);
            req.SetRequestHeader("X-Protocol-Version", protocolVersion);

            if (!string.IsNullOrEmpty(metaHash))
                req.SetRequestHeader("X-Meta-Hash", metaHash);

            if (!string.IsNullOrEmpty(AccessToken))
                req.SetRequestHeader("Authorization", $"Bearer {AccessToken}");
        }

        static void Complete(UnityWebRequest req, Action<bool, string> onComplete)
        {
            var body = req.downloadHandler?.text ?? "";
            if (req.result == UnityWebRequest.Result.Success)
                onComplete?.Invoke(true, body);
            else
                onComplete?.Invoke(false, string.IsNullOrEmpty(body) ? req.error : body);
        }
    }
}
