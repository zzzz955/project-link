namespace ProjectLink.Domain.Entities;

public class StageBestRecord
{
    public string         UserId          { get; set; } = default!;
    public int            StageId         { get; set; }
    public long           BestClearTimeMs { get; set; }
    public int            BestScore       { get; set; }
    public DateTimeOffset ClearedAt       { get; set; }
}
