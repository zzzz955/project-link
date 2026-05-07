namespace ProjectLink.Contracts.Progress;

public class StageProgressEntry
{
    public int     StageId    { get; set; }
    public int     Stars      { get; set; }
    public bool    IsUnlocked { get; set; }
    public string? ClearedAt  { get; set; }
}

public class ProgressResponse
{
    public List<StageProgressEntry> Stages { get; set; } = new();
}
