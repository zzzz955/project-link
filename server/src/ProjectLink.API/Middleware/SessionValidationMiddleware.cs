using System.Security.Claims;
using ProjectLink.Application.Session;
using ProjectLink.Application.UserProfile;

namespace ProjectLink.API.Middleware;

public class SessionValidationMiddleware
{
    private readonly RequestDelegate _next;

    public SessionValidationMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext ctx, SessionService sessionService, UserProfileService userProfileService)
    {
        if (ctx.User.Identity?.IsAuthenticated != true)
        {
            await _next(ctx);
            return;
        }

        var userId    = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var sessionId = ctx.User.FindFirstValue("session_id");
        var isMockAuth = ctx.User.FindFirstValue("mock_auth") == "true";

        if (userId is null || sessionId is null)
        {
            ctx.Response.StatusCode  = 401;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsJsonAsync(new { reason = "session_invalidated" });
            return;
        }

        if (!isMockAuth && !await sessionService.ValidateSessionAsync(userId, sessionId))
        {
            // Platform JWT is valid (passed auth middleware) but session not yet in local store — sync it.
            var expStr = ctx.User.FindFirstValue("exp");
            if (expStr is null || !long.TryParse(expStr, out var expUnix))
            {
                ctx.Response.StatusCode  = 401;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.WriteAsJsonAsync(new { reason = "session_invalidated" });
                return;
            }
            await sessionService.SyncSessionAsync(userId, sessionId, DateTimeOffset.FromUnixTimeSeconds(expUnix), ctx.RequestAborted);
        }

        // Upsert user_profiles on every authenticated request (Redis-cached after first insert)
        var displayName = ctx.User.FindFirstValue("name") ?? "guest_" + userId;
        await userProfileService.EnsureProfileAsync(userId, displayName, ctx.RequestAborted);

        await _next(ctx);
    }
}
