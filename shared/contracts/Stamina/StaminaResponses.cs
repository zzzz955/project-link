namespace ProjectLink.Contracts.Stamina;

public class StaminaResponse
{
    public int Current { get; set; }
    public int Max { get; set; }
    public string? NextRechargeAt { get; set; }  // ISO 8601, null if full
}

public class StaminaAdRewardResponse
{
    public int Current { get; set; }
    public int Added { get; set; }
    public string? NextRechargeAt { get; set; }
}

public class StaminaExtendResponse
{
    public int StaminaCurrent { get; set; }
    public long SoftBalanceAfter { get; set; }
}
