using Microsoft.AspNetCore.Diagnostics;
using System.Text.Json;

namespace LargeExcelProcessor.Api;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Unhandled exception");

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/problem+json";

        var response = new
        {
            type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            title = "An internal error occurred",
            status = 500
        };

        await JsonSerializer.SerializeAsync(httpContext.Response.Body, response, cancellationToken: cancellationToken);
        return true;
    }
}
