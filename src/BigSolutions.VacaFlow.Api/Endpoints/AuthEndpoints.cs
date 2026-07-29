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
                await httpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    BuildPrincipal(result.Value));
            }

            return result.ToCreatedResult(
                _ => "/api/auth/me",
                account => new RegisterAccountResponse(account.Id, account.FullName, account.Email, account.Role));
        })
        // Registration is anonymous by definition. Stated explicitly so that a
        // future fallback authorization policy cannot lock out the one endpoint
        // that has to be reachable without an account.
        .AllowAnonymous();
    }

    private static ClaimsPrincipal BuildPrincipal(RegisteredAccountDto account)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, account.Id.ToString()),
            new Claim(ClaimTypes.Role, account.Role),
        ], CookieAuthenticationDefaults.AuthenticationScheme);

        return new ClaimsPrincipal(identity);
    }
}
