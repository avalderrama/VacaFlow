using BigSolutions.VacaFlow.Application;
using BigSolutions.VacaFlow.Infrastructure;

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
// builder.Services.AddScoped<ICurrentUser, CurrentUserAccessor>();   // WP 4.4

var app = builder.Build();

// Endpoint groups are mapped here as they are written. Work packages 4.2 to 6.3;
// see WBS.md §3.
app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
   .WithName("Health");

app.Run();

/// <summary>
/// Exposed so the architecture and functional test projects can reference this
/// assembly. Minimal APIs generate an internal entry point otherwise.
/// </summary>
public partial class Program;
