namespace Madalang.Platform.Auth.Contracts.V1;

public static class AuthClaims
{
    public const string Issuer = "iss";
    public const string Audience = "aud";
    public const string Subject = "sub";
    public const string JwtId = "jti";
    public const string IssuedAt = "iat";
    public const string Expiry = "exp";
    public const string NotBefore = "nbf";
    public const string KeyId = "kid";
    public const string SessionId = "session_id";
    public const string TokenFamilyId = "token_family_id";
    public const string AccountType = "account_type";
    public const string ClientId = "client_id";
    public const string Provider = "provider";
    public const string AccountState = "account_state";
    public const string TokenUse = "token_use";
}

public static class AuthClaimValues
{
    public static class AccountType
    {
        public const string Guest = "guest";
        public const string Google = "google";
    }

    public static class AccountState
    {
        public const string Active = "active";
        public const string Locked = "locked";
        public const string Banned = "banned";
    }

    public static class TokenUse
    {
        public const string Access = "access";
    }

    public static class Provider
    {
        public const string Guest = "guest";
        public const string Google = "google";
    }
}
