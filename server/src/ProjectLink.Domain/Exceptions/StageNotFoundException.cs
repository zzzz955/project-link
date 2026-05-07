namespace ProjectLink.Domain.Exceptions;

public class StageNotFoundException : DomainException
{
    public StageNotFoundException(int stageId)
        : base("STAGE_NOT_FOUND", $"Stage {stageId} not found.") { }
}
