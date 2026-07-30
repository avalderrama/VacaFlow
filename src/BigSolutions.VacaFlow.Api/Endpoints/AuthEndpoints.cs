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

        // Nothing to decide here (US-009): invalidating the cookie is a
        // framework concern, not a business rule, so there is no handler.
        // The fallback policy in Program.cs already requires a session on any
        // endpoint that does not opt out — RequireAuthorization() is stated
        // here too so the contract reads locally instead of only in a comment
        // in another file.
        group.MapPost("/logout", async (HttpContext httpContext) =>
        {
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.NoContent();
        })
        .RequireAuthorization();

        // Returns the caller's own identifier, name, email and role
        // (FR-AUT-009). It comes entirely from ICurrentUser inside the
        // handler — no route or query parameter carries it (FR-AUT-010).
        // Same reason as /logout for stating .RequireAuthorization()
        // explicitly even though the FallbackPolicy already covers it.
        group.MapGet("/me", async (
            GetCurrentUserHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(cancellationToken);
            return result.ToOkResult(ToResponse);
        })
        .RequireAuthorization();
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
