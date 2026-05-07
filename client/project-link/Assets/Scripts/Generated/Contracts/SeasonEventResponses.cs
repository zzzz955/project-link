namespace ProjectLink.Contracts.Event;

public class SeasonEventEntry
{
    public int    EventId     { get; set; }
    public string Name        { get; set; } = "";
    public string Type        { get; set; } = "";
    public string StartAt     { get; set; } = "";
    public string EndAt       { get; set; } = "";
    public string MetricLabel { get; set; } = "";
    public bool   IsActive    { get; set; }
    public bool   IsLocked    { get; set; }
}

public class ActiveEventsResponse
{
    public List<SeasonEventEntry> Events { get; set; } = new();
}
