using ProjectLink.Contracts.Common;
using ProjectLink.Domain.Exceptions;

namespace ProjectLink.API.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate                      _next;
    private readonly ILogger<GlobalExceptionMiddleware>  _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await _next(ctx);
        }
        catch (DomainException ex)
        {
            ctx.Response.StatusCode = ex switch
            {
                InsufficientFundsException
                    or InsufficientStaminaException
                    or InsufficientInventoryException    => 422,
                StageSessionNotFoundException
                    or StageNotFoundException            => 404,
                InvalidStageResultException              => 400,
                _                                       => 409,
            };
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsJsonAsync(new ErrorResponse { ErrorCode = ex.ErrorCode });
        }
        catch (Exception ex)
        {
            var correlationId = ctx.Items["CorrelationId"]?.ToString();
            _logger.LogError(ex, "Unhandled exception [{CorrelationId}] {Method} {Path}",
                correlationId, ctx.Request.Method, ctx.Request.Path);
            ctx.Response.StatusCode  = 500;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsJsonAsync(new ErrorResponse { ErrorCode = "INTERNAL_ERROR" });
        }
    }
}
