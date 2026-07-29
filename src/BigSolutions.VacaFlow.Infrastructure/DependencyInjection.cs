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

        // DbContext, repositories, unit of work, password hasher and the seeder
        // are registered here. Work packages 3.3 and 3.4 fill this in; see
        // WBS.md §3 and SAD.md §7.
        return services;
    }
}
