namespace ProjectLink.Domain.Entities;

public class Inventory
{
    public string UserId   { get; set; } = default!;
    public int    ItemId   { get; set; }
    public int    Quantity { get; set; }
}
