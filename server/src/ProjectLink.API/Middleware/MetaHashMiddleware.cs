using Microsoft.EntityFrameworkCore;
using ProjectLink.Infrastructure.Persistence;

namespace ProjectLink.API.Middleware;

public class MetaHashMiddleware
{
    private readonly RequestDelegate _next;

    public MetaHashMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext ctx, AppDbContext db)
    {
        if (!ctx.Request.Path.StartsWithSegments("/api/auth/session", StringComparison.OrdinalIgnoreCase))
        {
            await _next(ctx);
            return;
        }

        var clientVersion = ctx.Request.Headers["X-Client-Version"].FirstOrDefault() ?? "";
        var metaHash      = ctx.Request.Headers["X-Meta-Hash"].FirstOrDefault()       ?? "";

        var record = await db.Set<ProjectLink.Domain.Entities.ClientMeta>()
            .FirstOrDefaultAsync(m => m.ClientVersion == clientVersion);

        if (record is null || record.MetaHash != metaHash)
        {
            ctx.Response.StatusCode  = 403;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsJsonAsync(new { reason = "integrity_check_failed" });
            return;
        }

        await _next(ctx);
    }
}
