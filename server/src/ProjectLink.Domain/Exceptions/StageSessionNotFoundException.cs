namespace ProjectLink.Domain.Exceptions;

public class StageSessionNotFoundException : DomainException
{
    public StageSessionNotFoundException()
        : base("STAGE_SESSION_NOT_FOUND", "No active stage session found.") { }
}
