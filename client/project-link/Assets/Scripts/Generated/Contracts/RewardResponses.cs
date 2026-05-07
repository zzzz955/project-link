namespace ProjectLink.Contracts.Reward;

public class RewardGrantEntry
{
    public string RewardType { get; set; } = "";
    public int    RewardId   { get; set; }
    public int    Amount     { get; set; }
}

public class RewardInventoryUpdate
{
    public int ItemId        { get; set; }
    public int QuantityAfter { get; set; }
}

public class RewardClaimResponse
{
    public List<RewardGrantEntry>      RewardsGranted   { get; set; } = new();
    public long                        SoftBalanceAfter { get; set; }
    public List<RewardInventoryUpdate> InventoryUpdates { get; set; } = new();
}
