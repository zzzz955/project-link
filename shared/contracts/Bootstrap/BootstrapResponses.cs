#nullable enable
using System.Collections.Generic;

namespace ProjectLink.Contracts.Bootstrap
{

public class BootstrapConfigResponse
{
    public string  ClientVersion         { get; set; } = "";
    public string  RequiredClientVersion { get; set; } = "";
    public string  ProtocolVersion       { get; set; } = "";
    public string  DataSchemaVersion     { get; set; } = "";
    public string  MetaHash              { get; set; } = "";
    public string  ServerTimeUtc         { get; set; } = "";
    public bool    Maintenance           { get; set; }
    public string? MaintenanceMessage    { get; set; }
}

public class DataBundleResponse
{
    public string                     SchemaVersion { get; set; } = "";
    public string                     MetaHash      { get; set; } = "";
    public Dictionary<string, string> Files         { get; set; } = new Dictionary<string, string>();
}
}
