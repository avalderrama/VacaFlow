using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace BigSolutions.VacaFlow.Api.FunctionalTests;

/// <summary>
/// Boots the real composition root (<see cref="Program"/>) — migrations,
/// DI wiring, the FallbackPolicy, all of it — against a temporary SQLite file
/// per factory instance, the same pattern
/// <c>Infrastructure.IntegrationTests.SqliteDatabaseFixture</c> uses for the
/// same reason (CA-TST-004): a real database, never a mock of the ORM.
/// </summary>
public sealed class VacaFlowApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath =
        Path.Combine(Path.GetTempPath(), $"vacaflow-functional-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:VacaFlow"] = $"Data Source={_databasePath}",
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
        {
            return;
        }

        // Sqlite's connection pool keeps a native file handle open even after
        // the host shuts down. Without this, deleting the file races the pool
        // and intermittently throws IOException.
        SqliteConnection.ClearAllPools();

        foreach (var suffix in new[] { "", "-shm", "-wal" })
        {
            var path = _databasePath + suffix;
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
