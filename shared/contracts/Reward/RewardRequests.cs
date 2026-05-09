#nullable enable

namespace ProjectLink.Contracts.Reward
{

public class RewardClaimRequest
{
    public string RewardSource { get; set; } = "";  // "ad_video" | "daily_login" | etc.
    public string RewardToken  { get; set; } = "";  // ad or server-issued verification token
    public int    Multiplier   { get; set; } = 1;
}
}
