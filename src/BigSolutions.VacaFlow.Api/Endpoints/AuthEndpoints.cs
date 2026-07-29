using System.Security.Claims;
using BigSolutions.VacaFlow.Api.Contracts;
using BigSolutions.VacaFlow.Api.ErrorHandling;
using BigSolutions.VacaFlow.Application.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace BigSolutions.VacaFlow.Api.Endpoints;

internal static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth");

        // Receives, delegates, maps — no business conditional (CA-PRE-001).
        group.MapPost("/register", async (
            RegisterAccountContract contract,
            RegisterEmployeeHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var command = new RegisterEmployeeCommand(
                contract.FullName, contract.Email, contract.Password, contract.Role);

            var result = await handler.Handle(command, cancellationToken);

            if (result.IsSuccess)
            {
                await SignInAsync(httpContext, result.Value);
            }

            return result.ToCreatedResult(_ => "/api/auth/me", ToResponse);
        })
        // Registration is anonymous by definition. Stated explicitly so that a
        // future fallback authorization policy cannot lock out the one endpoint
        // that has to be reachable without an account.
        .AllowAnonymous();

        group.MapPost("/login", async (
            SignInContract contract,
            SignInHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(
                new SignInCommand(contract.Email, contract.Password), cancellationToken);

            if (result.IsSuccess)
            {
                await SignInAsync(httpContext, result.Value);
            }

            return result.ToOkResult(ToResponse);
        })
        .AllowAnonymous();
    }

    private static Task SignInAsync(HttpContext httpContext, AuthenticatedUserDto user) =>
        httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            BuildPrincipal(user));

    private static AuthenticatedUserResponse ToResponse(AuthenticatedUserDto user) =>
        new(user.Id, user.FullName, user.Email, user.Role);

    private static ClaimsPrincipal BuildPrincipal(AuthenticatedUserDto user)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Role, user.Role),
        ], CookieAuthenticationDefaults.AuthenticationScheme);

        return new ClaimsPrincipal(identity);
    }
}
