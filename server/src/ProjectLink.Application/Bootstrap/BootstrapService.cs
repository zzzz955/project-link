using ProjectLink.Contracts.Bootstrap;
using ProjectLink.Domain.Interfaces;

namespace ProjectLink.Application.Bootstrap;

public class BootstrapService
{
    private readonly IConfiguration    _config;
    private readonly IStaticDataService _staticData;

    public BootstrapService(IConfiguration config, IStaticDataService staticData)
    {
        _config     = config;
        _staticData = staticData;
    }

    public BootstrapConfigResponse Get()
    {
        return new BootstrapConfigResponse
        {
            ClientVersion         = _config["Bootstrap:ClientVersion"]         ?? "1.0.0",
            RequiredClientVersion = _config["Bootstrap:RequiredClientVersion"] ?? "1.0.0",
            ProtocolVersion       = _config["Bootstrap:ProtocolVersion"]       ?? "1.0.0",
            DataSchemaVersion     = _staticData.DataSchemaVersion,
            MetaHash              = _staticData.MetaHash,
            ServerTimeUtc         = DateTimeOffset.UtcNow.ToString("O"),
            Maintenance           = _config.GetValue<bool>("Bootstrap:Maintenance"),
            MaintenanceMessage    = _config["Bootstrap:MaintenanceMessage"],
        };
    }
}
