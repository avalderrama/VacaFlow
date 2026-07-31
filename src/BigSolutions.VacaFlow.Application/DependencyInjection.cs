using BigSolutions.VacaFlow.Application.AbsenceTypes;
using BigSolutions.VacaFlow.Application.Authentication;
using BigSolutions.VacaFlow.Application.Requests;
using Microsoft.Extensions.DependencyInjection;

namespace BigSolutions.VacaFlow.Application;

/// <summary>
/// The application layer's only public registration surface (CA-CFG-002).
/// Everything it registers is resolved by constructor injection; nothing in
/// this layer ever asks the container for a service (CA-CFG-003).
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the use case handlers. Each is a plain class injected directly
    /// into its endpoint — there is no mediator (ADR-002).
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<RegisterEmployeeHandler>();
        services.AddScoped<SignInHandler>();
        services.AddScoped<GetCurrentUserHandler>();
        services.AddScoped<ListAbsenceTypesHandler>();
        services.AddScoped<CreateRequestHandler>();
        services.AddScoped<UpdateRequestHandler>();
        services.AddScoped<GetRequestByIdHandler>();
        services.AddScoped<SubmitRequestHandler>();
        services.AddScoped<CancelRequestHandler>();

        // Further handlers are registered here as they are written, one line
        // each. Work packages 4.3 to 6.2 fill this in; see WBS.md §3.
        return services;
    }
}
