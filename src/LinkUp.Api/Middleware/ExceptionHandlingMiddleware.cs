using System.Text.Json;
using Serilog;

namespace LinkUp.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlingMiddleware(RequestDelegate next)
        => _next = next;

    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await _next(ctx);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unhandled exception for {Method} {Path}", ctx.Request.Method, ctx.Request.Path);
            await HandleAsync(ctx, ex);
        }
    }

    private static Task HandleAsync(HttpContext ctx, Exception ex)
    {
        ctx.Response.ContentType = "application/json";
        ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var response = new
        {
            code = "INTERNAL_ERROR",
            message = "Ocorreu um erro interno. Tente novamente mais tarde."
        };

        return ctx.Response.WriteAsync(
            JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
}
