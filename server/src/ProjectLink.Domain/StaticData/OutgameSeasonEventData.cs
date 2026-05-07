namespace ProjectLink.Domain.StaticData;

public class OutgameSeasonEventData
{
    public int    EventId       { get; set; }
    public string Name          { get; set; } = "";
    public string Type          { get; set; } = "";
    public string StartAt       { get; set; } = "";
    public string EndAt         { get; set; } = "";
    public string MetricLabel   { get; set; } = "";
    public string RankingMetric { get; set; } = "";
}
