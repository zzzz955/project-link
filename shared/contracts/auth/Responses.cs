namespace Madalang.Platform.Auth.Contracts.V1;

public sealed record AuthTokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAt,
    DateTimeOffset RefreshTokenExpiresAt,
    string TokenType = "Bearer");

public sealed record AuthSessionResponse(
    Guid AccountId,
    Guid SessionId,
    string AccountType,
    string ClientId,
    AuthTokenResponse Tokens);

public sealed record AccountProfileResponse(
    Guid AccountId,
    string AccountType,
    string Status,
    string? DisplayName);

public sealed record RevokeResponse(bool Success, string? Reason);

public sealed record SessionStateResponse(
    Guid AccountId,
    Guid SessionId,
    string AccountType,
    string ClientId,
    bool IsActive,
    DateTimeOffset? ExpiresAt);
