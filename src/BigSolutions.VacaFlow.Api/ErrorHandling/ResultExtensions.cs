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

    private static IResult ToProblem(Error error) =>
        Results.Json(
            new { code = error.Code, message = error.Message, field = error.Field },
            statusCode: ErrorStatusMap.StatusFor(error.Code));
}
