namespace ProjectLink.Domain.Interfaces;

public class StageEndDbCommand
{
    public string  UserId          { get; set; } = default!;
    public int     StageId         { get; set; }
    public int     Stars           { get; set; }
    public int     Score           { get; set; }
    public long    AdjustedMs      { get; set; }
    public int     SoftReward      { get; set; }
    public int     StaminaRefund   { get; set; }
    public int     MaxStamina      { get; set; }
    public int     RechargeIntervalMinutes { get; set; }
    public int     MovesUsed       { get; set; }
    public int     MaxStages       { get; set; }
    public string  CorrelationId        { get; set; } = default!;
    public DateOnly ChallengeDate       { get; set; }
    public bool    IsDailyChallengeStage { get; set; }
}

public class StageEndDbResult
{
    public bool  IsBestRecord      { get; set; }
    public long  SoftBalanceAfter  { get; set; }
    public int   SoftRewardGranted { get; set; }
    public int   DailyPlayCount    { get; set; }
    public bool  NextStageUnlocked { get; set; }
    public long  TotalScore        { get; set; }
    public int   StagesCleared     { get; set; }
}

public interface IStageEndTransaction
{
    Task<StageEndDbResult> ExecuteAsync(StageEndDbCommand command, CancellationToken ct);
}
