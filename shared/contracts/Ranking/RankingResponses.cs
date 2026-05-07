namespace ProjectLink.Contracts.Ranking;

public class RankingEntry
{
    public int Rank { get; set; }
    public string UserId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public long Value { get; set; }
}

public class RankingListResponse
{
    public List<RankingEntry> Entries { get; set; } = new List<RankingEntry>();
    public RankingEntry? MyRank { get; set; }
}

public class MyRankEntry
{
    public int Rank { get; set; }
    public long Value { get; set; }
}

public class MyRankResponse
{
    public MyRankEntry? StagesCleared { get; set; }
    public MyRankEntry? TotalScore { get; set; }
}
