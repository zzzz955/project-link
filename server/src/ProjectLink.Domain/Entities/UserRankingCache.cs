namespace ProjectLink.Domain.Entities;

public class UserRankingCache
{
    public string         UserId        { get; set; } = default!;
    public long           TotalScore    { get; set; }
    public int            StagesCleared { get; set; }
    public DateTimeOffset UpdatedAt     { get; set; }
}
