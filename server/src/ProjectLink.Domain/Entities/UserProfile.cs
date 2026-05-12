namespace ProjectLink.Domain.Entities;

public class UserProfile
{
    public string         UserId              { get; set; } = default!;
    public string         DisplayName         { get; set; } = default!;
    public int            AvatarId            { get; set; } = 1;
    public DateTimeOffset AccountCreatedAt    { get; set; }
    public DateTimeOffset LastLoginAt         { get; set; }
    public int            MaxClearedStageId   { get; set; }
}
