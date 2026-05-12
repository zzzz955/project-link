namespace Madalang.Platform.Auth.Contracts.V1;

public sealed record AuthError(
    string Code,
    string Message,
    string? TraceId,
    bool Retryable,
    string? RequiredAction,
    IReadOnlyList<AuthFieldError>? Fields);

public sealed record AuthFieldError(string Field, string Code, string Message);

public static class AuthErrorCodes
{
    public const string InvalidRequest = "invalid_request";
    public const string InvalidCredentials = "invalid_credentials";
    public const string TokenExpired = "token_expired";
    public const string TokenRevoked = "token_revoked";
    public const string TokenReuseDetected = "token_reuse_detected";
    public const string RateLimited = "rate_limited";
    public const string AccountLocked = "account_locked";
    public const string AccountBanned = "account_banned";
    public const string ProviderTokenInvalid = "provider_token_invalid";
    public const string ProviderUnavailable = "provider_unavailable";
    public const string SigningKeyUnavailable = "signing_key_unavailable";
    public const string InternalError = "internal_error";
}
