namespace ProjectLink.Contracts.Daily;

public class DailyChallengeStreakTile
{
    public int  Day      { get; set; }
    public bool IsDone   { get; set; }
    public bool IsToday  { get; set; }
    public bool IsLocked { get; set; }
}

public class DailyChallengeRewardPreview
{
    public string RewardType { get; set; } = "";  // "SOFT_CURRENCY" | "ITEM"
    public int    RewardId   { get; set; }
    public int    Amount     { get; set; }
}

public class DailyChallengeResponse
{
    public bool   CompletedToday  { get; set; }
    public bool   CanComplete     { get; set; }
    public int    PlayCountToday  { get; set; }
    public int    PlayCountTarget { get; set; }
    public int    StreakDays      { get; set; }
    public string ResetAt         { get; set; } = "";
    public List<DailyChallengeStreakTile>    Tiles        { get; set; } = new();
    public List<DailyChallengeRewardPreview> TodayRewards { get; set; } = new();
}

public class DailyChallengeRewardGranted
{
    public string RewardType { get; set; } = "";
    public int    RewardId   { get; set; }
    public int    Amount     { get; set; }
}

public class DailyChallengeCompleteResponse
{
    public List<DailyChallengeRewardGranted> RewardsGranted  { get; set; } = new();
    public int                               StreakDays      { get; set; }
    public long                              SoftBalanceAfter { get; set; }
    public List<DailyInventoryUpdate>        InventoryUpdates { get; set; } = new();
}

public class DailyInventoryUpdate
{
    public int ItemId        { get; set; }
    public int QuantityAfter { get; set; }
}
