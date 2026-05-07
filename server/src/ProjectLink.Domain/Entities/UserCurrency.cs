namespace ProjectLink.Domain.Entities;

public class UserCurrency
{
    public string UserId    { get; set; } = default!;
    public long   SoftAmount { get; set; }
}
