using BigSolutions.VacaFlow.Application.Abstractions;
using BigSolutions.VacaFlow.Infrastructure.Identifiers;
using BigSolutions.VacaFlow.Infrastructure.Persistence;
using BigSolutions.VacaFlow.Infrastructure.Persistence.Repositories;
using BigSolutions.VacaFlow.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BigSolutions.VacaFlow.Infrastructure;

/// <summary>
/// The infrastructure layer's only public surface (CA-DEP-007, CA-CFG-002).
/// Every other type here is <c>internal sealed</c>, so the API physically
/// cannot construct a repository or reach a <c>DbContext</c> — which is what
/// turns CA-DEP-008 into a compile-time guarantee instead of a convention.
/// </summary>
public static class DependencyInjection
{
    internal const string ConnectionStringName = "VacaFlow";

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Fail fast on missing configuration rather than at the first query
        // (CA-CFG-006, NFR-OPS-004). No connection string is ever hardcoded
        // (CA-INF-007).
        var connectionString = configuration.GetConnectionString(ConnectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' is missing. " +
                "Set ConnectionStrings:VacaFlow in configuration before starting the API.");
        }

        services.AddDbContext<VacaFlowDbContext>(options => options.UseSqlite(connectionString));

        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<ICredentialStore, CredentialStore>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IDatabaseInitializer, DatabaseInitializer>();

        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IIdGenerator, GuidIdGenerator>();

        // The seeder joins this list with TE-003; not needed by US-007.
        return services;
    }
}
