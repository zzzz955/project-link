namespace ProjectLink.Domain.Exceptions;

public class InsufficientFundsException : DomainException
{
    public InsufficientFundsException()
        : base("INSUFFICIENT_FUNDS", "Insufficient currency balance.") { }
}
