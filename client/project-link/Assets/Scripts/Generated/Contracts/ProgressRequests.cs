namespace ProjectLink.Contracts.Progress;

public class BatchProgressRequest
{
    public List<int> StageIds { get; set; } = new();
}
