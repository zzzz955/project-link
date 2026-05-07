namespace ProjectLink.Domain.Entities;

public class CurrencyLog
{
    public long           Id            { get; set; }
    public string         UserId        { get; set; } = default!;
    public string         TransactionId { get; set; } = default!;
    public string         CurrencyType  { get; set; } = default!;
    public long           Delta         { get; set; }
    public long           BalanceBefore { get; set; }
    public long           BalanceAfter  { get; set; }
    public string         Reason        { get; set; } = default!;
    public string         CorrelationId { get; set; } = default!;
    public DateTimeOffset CreatedAt     { get; set; }
}
