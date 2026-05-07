namespace ProjectLink.Domain.Entities;

public class StaminaState
{
    public string         UserId          { get; set; } = default!;
    public int            Current         { get; set; }
    public DateTimeOffset LastRechargedAt { get; set; }
}
