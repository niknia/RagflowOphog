using System.Net;
using System.Text.Json;

namespace Rag.Api.Middleware;
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException)
        {
            context.Response.StatusCode = 499;
            await context.Response.WriteAsJsonAsync(new { error = "Request cancelled" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";
            var response = JsonSerializer.Serialize(new
            {
                error = ex.Message,
                type = ex.GetType().Name
            });
            await context.Response.WriteAsync(response);
        }
    }
}
