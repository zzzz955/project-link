#nullable enable

namespace ProjectLink.Contracts.Settings
{

public class PlayerSettingsUpdateRequest
{
    public bool?   BgmEnabled           { get; set; }
    public bool?   SfxEnabled           { get; set; }
    public bool?   HapticsEnabled       { get; set; }
    public bool?   NotificationsEnabled { get; set; }
    public string? Language             { get; set; }
}
}
