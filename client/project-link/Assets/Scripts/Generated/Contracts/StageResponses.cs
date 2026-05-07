namespace ProjectLink.Contracts.Stage;

public class StageStartResponse
{
    public string               SessionToken     { get; set; } = "";
    public string               ServerStartAt    { get; set; } = "";
    public int                  MoveLimit        { get; set; }   // 0 = unlimited
    public int                  TimeLimitSeconds { get; set; }   // 0 = unlimited
    public Dictionary<int, int> ItemCounts       { get; set; } = new();
    public int                  StaminaCurrent   { get; set; }
}

public class StageEndResponse
{
    public int   Score             { get; set; }
    public int   Stars             { get; set; }
    public long  AdjustedElapsedMs { get; set; }
    public bool  IsBestRecord      { get; set; }
    public long  SoftBalanceAfter  { get; set; }
    public int   SoftReward        { get; set; }
    public int   MovesUsed         { get; set; }
    public int   MoveLimit         { get; set; }
    public int?  NextStageId       { get; set; }
    public bool  NextStageUnlocked { get; set; }
}
