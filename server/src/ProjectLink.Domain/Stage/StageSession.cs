namespace ProjectLink.Domain.Stage;

public class StageSession
{
    public string UserId       { get; set; } = default!;
    public int    StageId      { get; set; }
    public string Token        { get; set; } = default!;  // returned to client as SessionToken
    public long   StartAtMs    { get; set; }
    public bool   IsSetupPhase  { get; set; } = true;
    public int    ExtensionCount { get; set; }
    public List<ItemUsedEntry> ItemsUsed { get; set; } = new();
}

public class ItemUsedEntry
{
    public int ItemId   { get; set; }
    public int Quantity { get; set; }
}
