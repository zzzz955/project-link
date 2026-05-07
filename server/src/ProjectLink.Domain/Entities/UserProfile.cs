namespace ProjectLink.Domain.Entities;

public class UserProfile
{
    public string         UserId           { get; set; } = default!;
    public string         DisplayName      { get; set; } = default!;
    public DateTimeOffset AccountCreatedAt { get; set; }
    public DateTimeOffset LastLoginAt      { get; set; }
}
