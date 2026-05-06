namespace ProjectLink.API.Middleware;

public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext ctx)
    {
        var correlationId = ctx.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                            ?? Guid.NewGuid().ToString();

        ctx.Items["CorrelationId"] = correlationId;
        ctx.Response.Headers["X-Correlation-ID"] = correlationId;

        await _next(ctx);
    }
}
