namespace ProjectLink.Domain.Exceptions;

public class StageAlreadyActiveException : DomainException
{
    public StageAlreadyActiveException()
        : base("STAGE_ALREADY_ACTIVE", "A stage is already in progress.") { }
}
