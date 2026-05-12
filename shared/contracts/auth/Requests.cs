namespace Madalang.Platform.Auth.Contracts.V1;

public sealed record GuestLoginRequest(string ClientId, string? DisplayName);

public sealed record GoogleLoginRequest(
    string ClientId,
    string IdToken,
    string? Nonce,
    string? GuestRefreshToken);

public sealed record RefreshRequest(string RefreshToken);

public sealed record LogoutRequest(string RefreshToken, string? Reason);

public sealed record RevokeRequest(string RefreshToken, string? Reason);

public sealed record RecoveryInitRequest(string ClientId, string Provider, string IdToken);

public sealed record AdminRevokeRequest(Guid AccountId, string Reason);

public sealed record SessionValidationRequest(string AccessToken);
