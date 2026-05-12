namespace Madalang.Platform.Auth.Contracts.V1;

public abstract record AuthEvent
{
    public Guid EventId { get; init; }
    public string EventType { get; init; } = string.Empty;
    public int Version { get; init; } = 1;
    public DateTimeOffset OccurredAt { get; init; }
    public Guid AccountId { get; init; }
    public Guid? SessionId { get; init; }
    public string? Reason { get; init; }
}

public static class AuthEventTypes
{
    public const string SessionRevoked = "session_revoked";
    public const string TokenFamilyCompromised = "token_family_compromised";
    public const string AccountLocked = "account_locked";
    public const string AccountRecovered = "account_recovered";
    public const string AccountBanned = "account_banned";
    public const string AccountUnbanned = "account_unbanned";
    public const string SigningKeyRotated = "signing_key_rotated";
    public const string UserSecurityStateChanged = "user_security_state_changed";
}

public sealed record SessionRevokedEvent : AuthEvent
{
    public Guid FamilyId { get; init; }
}

public sealed record TokenFamilyCompromisedEvent : AuthEvent
{
    public Guid FamilyId { get; init; }
}

public sealed record AccountLockedEvent : AuthEvent
{
    public DateTimeOffset? LockExpiresAt { get; init; }
    public int FailureCount { get; init; }
}

public sealed record AccountRecoveredEvent : AuthEvent
{
    public string RecoveryMethod { get; init; } = string.Empty;
}

public sealed record AccountBannedEvent : AuthEvent
{
    public DateTimeOffset? BanExpiresAt { get; init; }
}

public sealed record AccountUnbannedEvent : AuthEvent { }

public sealed record SigningKeyRotatedEvent : AuthEvent
{
    public string OldKid { get; init; } = string.Empty;
    public string NewKid { get; init; } = string.Empty;
    public DateTimeOffset OldKeyRetireAt { get; init; }
}

public sealed record UserSecurityStateChangedEvent : AuthEvent
{
    public string PreviousState { get; init; } = string.Empty;
    public string NewState { get; init; } = string.Empty;
}
