namespace ProjectLink.Domain.Exceptions;

public class StageNotInSetupPhaseException : DomainException
{
    public StageNotInSetupPhaseException()
        : base("STAGE_NOT_IN_SETUP_PHASE", "Stage setup phase has ended.") { }
}
