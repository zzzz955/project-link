#nullable enable

using System.Collections.Generic;
namespace ProjectLink.Contracts.Account
{

public class AccountMeResponse
{
    public string       UserId          { get; set; } = "";
    public string       DisplayName     { get; set; } = "";
    public bool         IsGuest         { get; set; }
    public List<string> LinkedProviders { get; set; } = new();
    public int          AvatarId        { get; set; }
    public string       CreatedAt       { get; set; } = "";
}

public class AuthResponse
{
    public string            AccessToken  { get; set; } = "";
    public string            RefreshToken { get; set; } = "";
    public string            ExpiresAt    { get; set; } = "";
    public AccountMeResponse Profile      { get; set; } = new();
}
}
