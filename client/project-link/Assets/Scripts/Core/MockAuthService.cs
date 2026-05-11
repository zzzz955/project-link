using System;

namespace ProjectLink.Core
{
    public enum MockAuthScenario { Success, Failure, SessionExpired }

    public sealed class MockAuthService : IAuthService
    {
        const string GuestToken = "mock:guest";

        public MockAuthScenario Scenario { get; set; } = MockAuthScenario.Success;

        string _token = "";

        public void EnsureAuth(Action<bool, string> onComplete)
        {
            switch (Scenario)
            {
                case MockAuthScenario.Failure:
                    onComplete?.Invoke(false, "AUTH_FAILED");
                    return;
                case MockAuthScenario.SessionExpired:
                    onComplete?.Invoke(false, "SESSION_EXPIRED");
                    return;
                default:
                    if (string.IsNullOrEmpty(_token))
                        _token = GuestToken;
                    onComplete?.Invoke(true, "");
                    break;
            }
        }

        public void Refresh(Action<bool, string> onComplete)
        {
            if (Scenario == MockAuthScenario.SessionExpired)
            {
                ClearToken();
                onComplete?.Invoke(false, "SESSION_EXPIRED");
                return;
            }
            _token = GuestToken;
            onComplete?.Invoke(true, "");
        }

        public string GetToken() => _token;

        public void SetToken(string token) => _token = token ?? "";

        public void ClearToken() => _token = "";
    }
}
