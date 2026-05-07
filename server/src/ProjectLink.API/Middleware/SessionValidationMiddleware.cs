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

        if (userId is null || sessionId is null || !await sessionService.ValidateSessionAsync(userId, sessionId))
        {
            ctx.Response.StatusCode  = 401;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsJsonAsync(new { reason = "session_invalidated" });
            return;
        }

        // Upsert user_profiles on every authenticated request (Redis-cached after first insert)
        var displayName = ctx.User.FindFirstValue("name") ?? "";
        await userProfileService.EnsureProfileAsync(userId, displayName, ctx.RequestAborted);

        await _next(ctx);
    }
}
