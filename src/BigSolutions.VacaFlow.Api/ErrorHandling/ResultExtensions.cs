using BigSolutions.VacaFlow.Domain.Primitives;
using Microsoft.AspNetCore.Http;

namespace BigSolutions.VacaFlow.Api.ErrorHandling;

/// <summary>
/// The single point that turns a Result into an HTTP response (CA-PRE-004).
/// No endpoint contains a try/catch or an if/else on IsSuccess of its own.
/// </summary>
internal static class ResultExtensions
{
    public static IResult ToHttpResult(this Result result) =>
        result.IsSuccess ? Results.NoContent() : ToProblem(result.Error);

    public static IResult ToCreatedResult<TValue, TBody>(
        this Result<TValue> result,
        Func<TValue, string> location,
        Func<TValue, TBody> body) =>
        result.IsSuccess
            ? Results.Created(location(result.Value), body(result.Value))
            : ToProblem(result.Error);

    public static IResult ToOkResult<TValue, TBody>(
        this Result<TValue> result,
        Func<TValue, TBody> body) =>
        result.IsSuccess
            ? Results.Ok(body(result.Value))
            : ToProblem(result.Error);

    private static object ErrorBody(Error error) =>
        new { code = error.Code, message = error.Message, field = error.Field };

    private static IResult ToProblem(Error error) =>
        Results.Json(ErrorBody(error), statusCode: ErrorStatusMap.StatusFor(error.Code));

    /// <summary>
    /// Same wire shape as <see cref="ToProblem"/>, for the one caller that
    /// cannot return an <see cref="IResult"/> — the cookie authentication
    /// events in the composition root, which only get an <see cref="HttpResponse"/>.
    /// A disconnected client cancels the write rather than surfacing as an
    /// unhandled exception the global handler would have to untangle.
    /// </summary>
    public static async Task WriteErrorAsync(this HttpResponse response, Error error)
    {
        response.StatusCode = ErrorStatusMap.StatusFor(error.Code);

        try
        {
            await response.WriteAsJsonAsync(ErrorBody(error), response.HttpContext.RequestAborted);
        }
        catch (OperationCanceledException)
        {
            // The client disconnected before the body finished writing.
        }
        catch (IOException)
        {
            // Kestrel surfaces an abrupt reset as this instead of a
            // cancellation — same "nothing left to write to" outcome.
        }
    }
}
