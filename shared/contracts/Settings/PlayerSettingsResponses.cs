#nullable enable

namespace ProjectLink.Contracts.Settings
{

public class PlayerSettingsResponse
{
    public bool   BgmEnabled           { get; set; } = true;
    public bool   SfxEnabled           { get; set; } = true;
    public bool   HapticsEnabled       { get; set; } = true;
    public bool   NotificationsEnabled { get; set; } = true;
    public string Language             { get; set; } = "EN";
}
}
