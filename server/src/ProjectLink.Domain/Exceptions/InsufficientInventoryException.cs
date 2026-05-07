namespace ProjectLink.Domain.Exceptions;

public class InsufficientInventoryException : DomainException
{
    public InsufficientInventoryException()
        : base("INSUFFICIENT_INVENTORY", "Insufficient item quantity.") { }
}
