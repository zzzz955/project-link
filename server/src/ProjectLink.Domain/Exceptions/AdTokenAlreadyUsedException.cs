namespace ProjectLink.Domain.Exceptions;

public class AdTokenAlreadyUsedException : DomainException
{
    public AdTokenAlreadyUsedException()
        : base("AD_TOKEN_ALREADY_USED", "Ad reward already claimed.") { }
}
