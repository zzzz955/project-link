#nullable enable

namespace ProjectLink.Contracts.Account
{

public class GuestLoginRequest { }

public class SocialLoginRequest
{
    public string Provider { get; set; } = "";  // "google" | "apple"
    public string IdToken  { get; set; } = "";
}

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = "";
}

public class LinkAccountRequest
{
    public string Provider { get; set; } = "";
    public string IdToken  { get; set; } = "";
}
}
