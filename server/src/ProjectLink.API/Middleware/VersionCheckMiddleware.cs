using Microsoft.Extensions.Configuration;

namespace ProjectLink.API.Middleware;

public class VersionCheckMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string          _allowedClient;
    private readonly string          _allowedProtocol;

    public VersionCheckMiddleware(RequestDelegate next, IConfiguration config)
    {
        _next            = next;
        _allowedClient   = config["App:AllowedClientVersion"]   ?? "";
        _allowedProtocol = config["App:AllowedProtocolVersion"] ?? "";
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        var path = ctx.Request.Path.Value ?? "";

        if (path.StartsWith("/health", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/scalar", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/openapi", StringComparison.OrdinalIgnoreCase))
        {
            await _next(ctx);
            return;
        }

        var clientVersion   = ctx.Request.Headers["X-Client-Version"].FirstOrDefault()   ?? "";
        var protocolVersion = ctx.Request.Headers["X-Protocol-Version"].FirstOrDefault() ?? "";

        if (clientVersion != _allowedClient)
        {
            ctx.Response.StatusCode  = 426;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsJsonAsync(new { reason = "version_mismatch", required = _allowedClient });
            return;
        }

        if (protocolVersion != _allowedProtocol)
        {
            ctx.Response.StatusCode  = 426;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsJsonAsync(new { reason = "version_mismatch", required = _allowedProtocol });
            return;
        }

        await _next(ctx);
    }
}
