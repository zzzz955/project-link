namespace ProjectLink.Contracts.Stage;

public class StageStartResponse
{
    public string SessionToken { get; set; } = "";
    public string ServerStartAt { get; set; } = "";  // ISO 8601
}

public class StageEndResponse
{
    public int Score { get; set; }
    public int Stars { get; set; }
    public long AdjustedElapsedMs { get; set; }
    public bool IsBestRecord { get; set; }
    public long SoftBalanceAfter { get; set; }
}
