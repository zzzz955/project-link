using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

namespace ProjectLink.Infrastructure.Security;

public class JwtPublicKeyCache : IHostedService, IDisposable
{
    private readonly HttpClient     _http;
    private readonly string         _jwksUrl;
    private List<SecurityKey>       _keys = new();
    private Timer?                  _timer;

    public JwtPublicKeyCache(IConfiguration config, HttpClient http)
    {
        var authority = config["Jwt:Authority"];
        if (string.IsNullOrWhiteSpace(authority))
        {
            throw new InvalidOperationException("Configuration error at configuration:Jwt:Authority: missing required value.");
        }

        _http    = http;
        _jwksUrl = $"{authority.TrimEnd('/')}/.well-known/jwks.json";
    }

    public IEnumerable<SecurityKey> GetKeys() => _keys;

    public async Task StartAsync(CancellationToken ct)
    {
        await RefreshAsync();
        _timer = new Timer(_ => _ = RefreshAsync(), null, TimeSpan.FromHours(24), TimeSpan.FromHours(24));
    }

    public Task StopAsync(CancellationToken ct)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    private async Task RefreshAsync()
    {
        try
        {
            var json = await _http.GetStringAsync(_jwksUrl);
            var doc  = JsonDocument.Parse(json);
            var keys = new List<SecurityKey>();

            foreach (var key in doc.RootElement.GetProperty("keys").EnumerateArray())
            {
                if (!key.TryGetProperty("kty", out var kty) || kty.GetString() != "RSA")
                    continue;

                var rsa = RSA.Create();
                rsa.ImportParameters(new RSAParameters
                {
                    Modulus  = Base64UrlDecode(key.GetProperty("n").GetString()!),
                    Exponent = Base64UrlDecode(key.GetProperty("e").GetString()!),
                });
                keys.Add(new RsaSecurityKey(rsa));
            }

            _keys = keys;
        }
        catch
        {
            // retain existing keys on failure
        }
    }

    private static byte[] Base64UrlDecode(string s)
        => Base64UrlEncoder.DecodeBytes(s);

    public void Dispose() => _timer?.Dispose();
}
