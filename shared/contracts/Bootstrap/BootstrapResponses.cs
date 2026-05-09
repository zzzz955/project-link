#nullable enable

namespace ProjectLink.Contracts.Bootstrap
{

public class BootstrapConfigResponse
{
    public string  ClientVersion         { get; set; } = "";
    public string  RequiredClientVersion { get; set; } = "";
    public string  ProtocolVersion       { get; set; } = "";
    public string  MetaHash              { get; set; } = "";
    public string  ServerTimeUtc         { get; set; } = "";
    public bool    Maintenance           { get; set; }
    public string? MaintenanceMessage    { get; set; }
}
}
