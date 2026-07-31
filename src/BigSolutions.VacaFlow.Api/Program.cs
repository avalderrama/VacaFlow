using BigSolutions.VacaFlow.Api.Endpoints;
using BigSolutions.VacaFlow.Api.ErrorHandling;
using BigSolutions.VacaFlow.Api.Security;
using BigSolutions.VacaFlow.Application;
using BigSolutions.VacaFlow.Application.Abstractions;
using BigSolutions.VacaFlow.Domain.Employees.Errors;
using BigSolutions.VacaFlow.Infrastructure;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;

// ---------------------------------------------------------------------------
// Composition root (CA-CFG-001). This file is the only place in the solution
// that knows every layer. It wires implementations to ports and does nothing
// else — no business logic lives here.
// ---------------------------------------------------------------------------

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// The clock is injected everywhere it is needed, so a test can fix it
// (CA-DOM-009, CA-CRS-002).
builder.Services.AddSingleton(TimeProvider.System);

// ICurrentUser is implemented in this project, not in Infrastructure: it reads
// claims from HttpContext, and keeping it here stops the web framework leaking
// inward (SAD §6.3).
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUserAccessor>();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "VacaFlow.Session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;

        // This is an API, not an MVC app with a login page: an unauthenticated
        // call must return 401/403 JSON, not a 302 redirect to a page that
        // does not exist (NFR-SEC-005).
        options.Events.OnRedirectToLogin = context =>
            context.Response.WriteErrorAsync(EmployeeErrors.NotAuthenticated);
        // No endpoint issues a role-based challenge yet (that starts with
        // US-021's manager-only actions), so there is no §7 code to attach a
        // body to. Give this the same treatment as OnRedirectToLogin once one
        // exists.
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

// FR-AUT-011: every endpoint requires an authenticated caller unless it opts
// out with AllowAnonymous(). A fallback policy makes that a structural
// guarantee instead of a convention every new endpoint has to remember.
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// Applying migrations at startup needs its own scope: the composition root is
// exempt from CA-CFG-003, but the resolved services still follow their normal
// scoped lifetime (FR-DAT-001).
using (var startupScope = app.Services.CreateScope())
{
    var initializer = startupScope.ServiceProvider.GetRequiredService<IDatabaseInitializer>();
    await initializer.InitializeAsync(CancellationToken.None);
}

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
   .WithName("Health")
   .AllowAnonymous();

app.MapAuthEndpoints();
app.MapAbsenceTypeEndpoints();

app.Run();

/// <summary>
/// Exposed so the architecture and functional test projects can reference this
/// assembly. Minimal APIs generate an internal entry point otherwise.
/// </summary>
public partial class Program;
