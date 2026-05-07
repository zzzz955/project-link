namespace ProjectLink.Domain.Exceptions;

public class InsufficientStaminaException : DomainException
{
    public InsufficientStaminaException()
        : base("INSUFFICIENT_STAMINA", "Insufficient stamina.") { }
}
