#nullable enable

namespace ProjectLink.Contracts.Stage
{

public class StageStartRequest { }

public class StageEndRequest
{
    public string SessionToken    { get; set; } = "";
    public string Result          { get; set; } = "";  // "success" | "fail"
    public long   ClientElapsedMs { get; set; }
    public int    MovesUsed       { get; set; }
}
}
