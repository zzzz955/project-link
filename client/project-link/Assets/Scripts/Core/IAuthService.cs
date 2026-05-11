using System;

namespace ProjectLink.Core
{
    public interface IAuthService
    {
        void EnsureAuth(Action<bool, string> onComplete);
        void Refresh(Action<bool, string> onComplete);
        string GetToken();
        void SetToken(string token);
        void ClearToken();
    }
}
