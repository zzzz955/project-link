namespace ProjectLink.Domain.Entities;

public class ActionLog
{
    public long           Id            { get; set; }
    public string         UserId        { get; set; } = default!;
    public string         ActionType    { get; set; } = default!;
    public string?        Payload       { get; set; }
    public string         CorrelationId { get; set; } = default!;
    public DateTimeOffset CreatedAt     { get; set; }
}
