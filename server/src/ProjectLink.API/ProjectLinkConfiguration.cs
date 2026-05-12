using System.Globalization;

namespace ProjectLink.API;

public sealed class ProjectLinkConfiguration
{
    public required string GameEnvironment { get; init; }
    public required DatabaseOptions Database { get; init; }
    public required RedisOptions Redis { get; init; }
    public required AuthOptions Auth { get; init; }
    public required AppOptions App { get; init; }
    public required RateLimitOptions RateLimit { get; init; }

    public static ProjectLinkConfiguration Load(IConfiguration configuration)
    {
        var loaded = new ProjectLinkConfiguration
        {
            GameEnvironment = EnvRequired("GAME_ENV"),
            Database = new DatabaseOptions
            {
                Host = EnvRequired("DB_HOST"),
                Port = EnvIntRequired("DB_PORT"),
                Name = EnvRequired("DB_NAME"),
                User = EnvRequired("DB_USER"),
                Password = EnvRequired("DB_PASSWORD")
            },
            Redis = new RedisOptions
            {
                Host = EnvRequired("REDIS_HOST"),
                Port = EnvIntRequired("REDIS_PORT")
            },
            Auth = new AuthOptions
            {
                UseMock = EnvBoolRequired("AUTH_USE_MOCK"),
                JwtAuthority = EnvAbsoluteUriRequired("JWT_AUTHORITY"),
                JwtAudience = EnvRequired("JWT_AUDIENCE")
            },
            App = new AppOptions
            {
                ClientId = EnvRequired("APP_CLIENT_ID"),
                AllowedClientVersion = EnvRequired("APP_ALLOWED_CLIENT_VERSION"),
                AllowedProtocolVersion = EnvRequired("APP_ALLOWED_PROTOCOL_VERSION")
            },
            RateLimit = new RateLimitOptions
            {
                StageStartPerHour = ConfigIntRequired(configuration, "RateLimit:StageStartPerHour"),
                RankingPerMinute = ConfigIntRequired(configuration, "RateLimit:RankingPerMinute")
            }
        };

        loaded.ApplyTo(configuration);
        return loaded;
    }

    public void ApplyTo(IConfiguration configuration)
    {
        configuration["ConnectionStrings:MySQL"] = Database.ConnectionString;
        configuration["Redis:Connection"] = Redis.ConnectionString;
        configuration["Jwt:Authority"] = Auth.JwtAuthority;
        configuration["Jwt:Audience"] = Auth.JwtAudience;
        configuration["Auth:UseMock"] = Auth.UseMock.ToString(CultureInfo.InvariantCulture);
        configuration["App:ClientId"] = App.ClientId;
        configuration["App:AllowedClientVersion"] = App.AllowedClientVersion;
        configuration["App:AllowedProtocolVersion"] = App.AllowedProtocolVersion;
    }

    public sealed class DatabaseOptions
    {
        public required string Host { get; init; }
        public required int Port { get; init; }
        public required string Name { get; init; }
        public required string User { get; init; }
        public required string Password { get; init; }

        public string ConnectionString => $"Server={Host};Port={Port};Database={Name};User={User};Password={Password}";
    }

    public sealed class RedisOptions
    {
        public required string Host { get; init; }
        public required int Port { get; init; }

        public string ConnectionString => $"{Host}:{Port}";
    }

    public sealed class AuthOptions
    {
        public required bool UseMock { get; init; }
        public required string JwtAuthority { get; init; }
        public required string JwtAudience { get; init; }
    }

    public sealed class AppOptions
    {
        public required string ClientId { get; init; }
        public required string AllowedClientVersion { get; init; }
        public required string AllowedProtocolVersion { get; init; }
    }

    public sealed class RateLimitOptions
    {
        public required int StageStartPerHour { get; init; }
        public required int RankingPerMinute { get; init; }
    }

    private static string EnvRequired(string key)
    {
        var value = Environment.GetEnvironmentVariable(key);
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        throw new InvalidOperationException($"Configuration error at env:{key}: missing required environment variable.");
    }

    private static int EnvIntRequired(string key)
    {
        var value = EnvRequired(key);
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException($"Configuration error at env:{key}: `{value}` is not a valid integer.");
    }

    private static bool EnvBoolRequired(string key)
    {
        var value = EnvRequired(key);
        if (bool.TryParse(value, out var parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException($"Configuration error at env:{key}: `{value}` is not a valid boolean.");
    }

    private static string EnvAbsoluteUriRequired(string key)
    {
        var value = EnvRequired(key);
        if (Uri.TryCreate(value, UriKind.Absolute, out _))
        {
            return value;
        }

        throw new InvalidOperationException($"Configuration error at env:{key}: `{value}` is not a valid absolute URI.");
    }

    private static int ConfigIntRequired(IConfiguration configuration, string key)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Configuration error at configuration:{key}: missing required value.");
        }

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException($"Configuration error at configuration:{key}: `{value}` is not a valid integer.");
    }
}
