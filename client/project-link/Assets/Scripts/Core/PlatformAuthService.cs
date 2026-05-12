using System;
using System.Collections;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace ProjectLink.Core
{
    public sealed class PlatformAuthService : IAuthService
    {
        const string AccessTokenKey = "PlatformAuth.AccessToken";
        const string RefreshTokenKey = "PlatformAuth.RefreshToken";
        const string AccessExpiresAtKey = "PlatformAuth.AccessExpiresAt";
        const string ProviderKey = "PlatformAuth.Provider";
        const string ClientIdKey = "PlatformAuth.ClientId";

        readonly MonoBehaviour _runner;
        readonly string _baseUrl;
        readonly string _clientVersion;
        readonly string _protocolVersion;
        readonly string _clientId;
        readonly ITokenStorage _storage;
        readonly bool _httpLogging;

        string _accessToken;
        string _refreshToken;
        DateTimeOffset _accessExpiresAt;
        string _provider;

        public PlatformAuthService(MonoBehaviour runner, string baseUrl, string clientVersion, string protocolVersion, AppEnvironment env = AppEnvironment.Dev, bool httpLogging = true)
        {
            _runner = runner;
            _baseUrl = string.IsNullOrEmpty(baseUrl) ? AppConfig.DevPlatformAuthUrl : baseUrl.TrimEnd('/');
            _clientVersion = clientVersion ?? "";
            _protocolVersion = protocolVersion ?? "";
            _storage = env == AppEnvironment.Prod ? (ITokenStorage)new SecureTokenStorage() : new PlayerPrefsTokenStorage();
            _httpLogging = httpLogging;
            _clientId = LoadOrCreateClientId();
            LoadSession();
        }

        public bool HasStoredSession => !string.IsNullOrEmpty(_refreshToken);
        public string Provider => _provider;

        public void EnsureAuth(Action<bool, string> onComplete)
        {
            if (HasUsableAccessToken())
            {
                onComplete?.Invoke(true, "");
                return;
            }

            if (!string.IsNullOrEmpty(_refreshToken))
            {
                Refresh(onComplete);
                return;
            }

            LoginGuest(onComplete);
        }

        public void LoginGuest(Action<bool, string> onComplete)
        {
            var request = new GuestLoginRequest
            {
                ClientId = _clientId,
                DisplayName = null,
            };
            Send<GuestLoginRequest, AuthSessionResponse>("/auth/guest", request, (ok, error, session) =>
                CompleteSession(ok, error, "guest", session, onComplete));
        }

        public void LoginGoogle(string idToken, string nonce, Action<bool, string> onComplete)
        {
            if (string.IsNullOrWhiteSpace(idToken))
            {
                onComplete?.Invoke(false, "AUTH_PROVIDER_TOKEN_MISSING");
                return;
            }

            var request = new GoogleLoginRequest
            {
                ClientId = _clientId,
                IdToken = idToken,
                Nonce = string.IsNullOrEmpty(nonce) ? null : nonce,
                GuestRefreshToken = string.IsNullOrEmpty(_refreshToken) ? null : _refreshToken,
            };
            Send<GoogleLoginRequest, AuthSessionResponse>("/auth/google", request, (ok, error, session) =>
                CompleteSession(ok, error, "google", session, onComplete));
        }

        public void Refresh(Action<bool, string> onComplete)
        {
            if (string.IsNullOrEmpty(_refreshToken))
            {
                onComplete?.Invoke(false, "SESSION_EXPIRED");
                return;
            }

            Send<RefreshRequest, AuthSessionResponse>("/auth/refresh", new RefreshRequest { RefreshToken = _refreshToken },
                (ok, error, session) =>
                {
                    if (!ok)
                    {
                        ClearToken();
                        onComplete?.Invoke(false, string.IsNullOrEmpty(error) ? "SESSION_EXPIRED" : error);
                        return;
                    }

                    CompleteSession(true, "", string.IsNullOrEmpty(_provider) ? "refresh" : _provider, session, onComplete);
                });
        }

        public void Logout(Action<bool, string> onComplete)
        {
            if (string.IsNullOrEmpty(_refreshToken))
            {
                ClearToken();
                onComplete?.Invoke(true, "");
                return;
            }

            Send<LogoutRequest, EmptyResponse>("/auth/logout",
                new LogoutRequest { RefreshToken = _refreshToken, Reason = "client_logout" },
                (ok, error, _) =>
                {
                    ClearToken();
                    onComplete?.Invoke(ok, error);
                });
        }

        public string GetToken() => _accessToken;

        public void SetToken(string token)
        {
            _accessToken = token ?? "";
            _storage.Set(AccessTokenKey, _accessToken);
            _storage.Save();
        }

        public void ClearToken()
        {
            _accessToken = "";
            _refreshToken = "";
            _accessExpiresAt = default;
            _provider = "";
            _storage.Delete(AccessTokenKey);
            _storage.Delete(RefreshTokenKey);
            _storage.Delete(AccessExpiresAtKey);
            _storage.Delete(ProviderKey);
            _storage.Save();
            UiEventBus.Publish(new AuthStateChanged(false, ""));
        }

        void CompleteSession(bool ok, string error, string provider, AuthSessionResponse session, Action<bool, string> onComplete)
        {
            if (!ok || session?.Tokens == null)
            {
                onComplete?.Invoke(false, string.IsNullOrEmpty(error) ? "AUTH_FAILED" : error);
                return;
            }

            _accessToken = session.Tokens.AccessToken ?? "";
            _refreshToken = session.Tokens.RefreshToken ?? "";
            _accessExpiresAt = ParseTime(session.Tokens.AccessTokenExpiresAt);
            _provider = provider ?? "";

            _storage.Set(AccessTokenKey, _accessToken);
            _storage.Set(RefreshTokenKey, _refreshToken);
            _storage.Set(AccessExpiresAtKey, _accessExpiresAt.ToString("O"));
            _storage.Set(ProviderKey, _provider);
            _storage.Save();
            UiEventBus.Publish(new AuthStateChanged(!string.IsNullOrEmpty(_accessToken), _provider));
            onComplete?.Invoke(true, "");
        }

        void Send<TRequest, TResponse>(string endpoint, TRequest body, Action<bool, string, TResponse> onComplete)
        {
            if (_runner == null)
            {
                onComplete?.Invoke(false, "NETWORK_UNAVAILABLE", default);
                return;
            }

            _runner.StartCoroutine(SendRoutine(endpoint, body, onComplete));
        }

        IEnumerator SendRoutine<TRequest, TResponse>(string endpoint, TRequest body, Action<bool, string, TResponse> onComplete)
        {
            var url = $"{_baseUrl}/{endpoint.TrimStart('/')}";
            using var req = new UnityWebRequest(url, "POST");
            var payload = JsonConvert.SerializeObject(body);
            req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(payload));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("X-Client-Version", _clientVersion);
            req.SetRequestHeader("X-Protocol-Version", _protocolVersion);

            yield return req.SendWebRequest();

            string response = req.downloadHandler?.text ?? "";
            if (req.result == UnityWebRequest.Result.Success)
            {
                if (_httpLogging)
                {
                    var sb = $"[AUTH] POST {url} → {req.responseCode}\nreq: {Clip(payload)}";
                    if (!string.IsNullOrEmpty(response)) sb += $"\nres: {Clip(response)}";
                    Debug.Log(sb);
                }
                if (req.responseCode == 204 || string.IsNullOrWhiteSpace(response))
                {
                    onComplete?.Invoke(true, "", default);
                    yield break;
                }

                try
                {
                    onComplete?.Invoke(true, "", JsonConvert.DeserializeObject<TResponse>(response));
                }
                catch (Exception ex)
                {
                    onComplete?.Invoke(false, ex.Message, default);
                }
                yield break;
            }

            var errorCode = ParseError(response, req.responseCode);
            var status = req.responseCode > 0 ? req.responseCode.ToString() : req.result.ToString();
            if (_httpLogging)
            {
                var sb = $"[AUTH] POST {url} → {status} [{errorCode}]\nreq: {Clip(payload)}";
                if (!string.IsNullOrEmpty(response)) sb += $"\nerr: {Clip(response)}";
                if (req.responseCode >= 500 || req.responseCode == 0)
                    Debug.LogError(sb);
                else
                    Debug.LogWarning(sb);
            }
            onComplete?.Invoke(false, errorCode, default);
        }

        static string Clip(string s, int max = 300) =>
            s.Length <= max ? s : s[..max] + $"…({s.Length})";

        void LoadSession()
        {
            _accessToken = _storage.Get(AccessTokenKey);
            _refreshToken = _storage.Get(RefreshTokenKey);
            _provider = _storage.Get(ProviderKey);
            _accessExpiresAt = ParseTime(_storage.Get(AccessExpiresAtKey));
        }

        bool HasUsableAccessToken()
        {
            return !string.IsNullOrEmpty(_accessToken)
                && (_accessExpiresAt == default || _accessExpiresAt > DateTimeOffset.UtcNow.AddMinutes(1));
        }

        string LoadOrCreateClientId()
        {
            var id = _storage.Get(ClientIdKey);
            if (!string.IsNullOrEmpty(id)) return id;

            id = $"project-link:{Guid.NewGuid():N}";
            _storage.Set(ClientIdKey, id);
            _storage.Save();
            return id;
        }

        static DateTimeOffset ParseTime(string value)
        {
            return DateTimeOffset.TryParse(value, out var parsed) ? parsed : default;
        }

        static string ParseError(string payload, long statusCode)
        {
            if (statusCode == 401) return "SESSION_EXPIRED";

            try
            {
                var error = JsonConvert.DeserializeObject<AuthErrorResponse>(payload);
                if (!string.IsNullOrEmpty(error?.Code))
                    return NormalizeError(error.Code);
            }
            catch
            {
                // Fall through to status mapping.
            }

            return statusCode == 429 ? "RATE_LIMITED" : "AUTH_FAILED";
        }

        static string NormalizeError(string code)
        {
            return string.IsNullOrEmpty(code) ? "AUTH_FAILED" : code.ToUpperInvariant();
        }

        sealed class GuestLoginRequest
        {
            public string ClientId { get; set; }
            public string DisplayName { get; set; }
        }

        sealed class GoogleLoginRequest
        {
            public string ClientId { get; set; }
            public string IdToken { get; set; }
            public string Nonce { get; set; }
            public string GuestRefreshToken { get; set; }
        }

        sealed class RefreshRequest
        {
            public string RefreshToken { get; set; }
        }

        sealed class LogoutRequest
        {
            public string RefreshToken { get; set; }
            public string Reason { get; set; }
        }

        sealed class AuthSessionResponse
        {
            public string AccountId { get; set; }
            public string SessionId { get; set; }
            public string AccountType { get; set; }
            public string ClientId { get; set; }
            public AuthTokenResponse Tokens { get; set; }
        }

        sealed class AuthTokenResponse
        {
            public string AccessToken { get; set; }
            public string RefreshToken { get; set; }
            public string AccessTokenExpiresAt { get; set; }
            public string RefreshTokenExpiresAt { get; set; }
            public string TokenType { get; set; }
        }

        sealed class AuthErrorResponse
        {
            public string Code { get; set; }
            public string Message { get; set; }
        }

        sealed class EmptyResponse { }
    }
}
