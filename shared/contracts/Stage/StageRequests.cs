namespace ProjectLink.Contracts.Stage;

public class StageStartRequest { }

public class StageEndRequest
{
    public string Result { get; set; } = "";         // "success" | "fail"
    public long ClientElapsedMs { get; set; }
}
