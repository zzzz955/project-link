namespace ProjectLink.Domain.Interfaces;

public interface ISessionRepository
{
    Task<string?> GetCurrentSessionIdAsync(string userId, CancellationToken ct);
    Task CreateSessionAsync(string userId, string sessionId, DateTimeOffset expiresAt, CancellationToken ct);
    // Returns false if session_id already exists (UQ violation) — safe to ignore.
    Task<bool> TryCreateSessionAsync(string userId, string sessionId, DateTimeOffset expiresAt, CancellationToken ct);
    Task InvalidateAsync(string userId, CancellationToken ct);
}
