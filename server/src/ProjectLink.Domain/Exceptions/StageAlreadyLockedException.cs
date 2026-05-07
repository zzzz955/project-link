namespace ProjectLink.Domain.Exceptions;

public class StageAlreadyLockedException : DomainException
{
    public StageAlreadyLockedException()
        : base("STAGE_ALREADY_LOCKED", "Stage setup is already locked.") { }
}
