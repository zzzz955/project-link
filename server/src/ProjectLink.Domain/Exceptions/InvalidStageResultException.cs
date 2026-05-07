namespace ProjectLink.Domain.Exceptions;

public class InvalidStageResultException : DomainException
{
    public InvalidStageResultException()
        : base("INVALID_STAGE_RESULT", "Invalid stage result value.") { }
}
