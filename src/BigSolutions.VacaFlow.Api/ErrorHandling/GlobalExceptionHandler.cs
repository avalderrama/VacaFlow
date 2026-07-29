using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace BigSolutions.VacaFlow.Api.ErrorHandling;

/// <summary>
/// The single catch-all for anything a Result did not anticipate. Returns a
/// generic message that leaks no stack trace, table name or provider detail
/// (NFR-USA-003).
/// </summary>
internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Unhandled exception while processing {Path}", httpContext.Request.Path);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(
            new { code = "VF-SRV-001", message = "An unexpected error occurred." },
            cancellationToken);

        return true;
    }
}
