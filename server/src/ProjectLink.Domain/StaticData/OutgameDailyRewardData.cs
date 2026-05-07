namespace ProjectLink.Domain.StaticData;

public class OutgameDailyRewardData
{
    public int    StreakDay  { get; set; }
    public string RewardType { get; set; } = "";  // "SOFT_CURRENCY" | "ITEM"
    public int    RewardId   { get; set; }
    public int    Amount     { get; set; }
}
