using Microsoft.AspNetCore.Mvc.Filters;

namespace Rag.Api.Filters;
public class RequestLoggingFilter : IActionFilter
{
    private readonly ILogger<RequestLoggingFilter> _logger;

    public RequestLoggingFilter(ILogger<RequestLoggingFilter> logger) => _logger = logger;

    public void OnActionExecuting(ActionExecutingContext context)
    {
        _logger.LogInformation("Request: {Method} {Path}", context.HttpContext.Request.Method, context.HttpContext.Request.Path);
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        _logger.LogInformation("Response: {StatusCode}", context.HttpContext.Response.StatusCode);
    }
}
