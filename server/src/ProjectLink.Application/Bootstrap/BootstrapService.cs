using ProjectLink.Contracts.Bootstrap;

namespace ProjectLink.Application.Bootstrap;

public class BootstrapService
{
    private readonly IConfiguration _config;

    public BootstrapService(IConfiguration config) => _config = config;

    public BootstrapConfigResponse Get()
    {
        return new BootstrapConfigResponse
        {
            ClientVersion         = _config["Bootstrap:ClientVersion"]         ?? "1.0.0",
            RequiredClientVersion = _config["Bootstrap:RequiredClientVersion"] ?? "1.0.0",
            ProtocolVersion       = _config["Bootstrap:ProtocolVersion"]       ?? "1.0.0",
            MetaHash              = _config["Bootstrap:MetaHash"]              ?? "",
            ServerTimeUtc         = DateTimeOffset.UtcNow.ToString("O"),
            Maintenance           = _config.GetValue<bool>("Bootstrap:Maintenance"),
            MaintenanceMessage    = _config["Bootstrap:MaintenanceMessage"],
        };
    }
}
