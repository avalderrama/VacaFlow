using BigSolutions.VacaFlow.Application.Abstractions;
using BigSolutions.VacaFlow.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BigSolutions.VacaFlow.Infrastructure.IntegrationTests.Persistence;

/// <summary>
/// A real, temporary SQLite file per test class (CA-TST-004) — never a mock of
/// the ORM. Everything under test is reached only through the ports Application
/// declares, resolved from a real container built by AddInfrastructure(),
/// exactly as Program.cs builds it. No internal Infrastructure type is
/// referenced directly, so no InternalsVisibleTo is needed.
/// </summary>
public sealed class SqliteDatabaseFixture : IAsyncLifetime
{
    private readonly string _databasePath =
        Path.Combine(Path.GetTempPath(), $"vacaflow-test-{Guid.NewGuid():N}.db");

    private ServiceProvider? _serviceProvider;

    /// <summary>
    /// Lets a test fix the clock (CA-CRS-002). Settable rather than a
    /// constructor parameter because xUnit requires an IClassFixture type to
    /// expose exactly one public constructor.
    /// </summary>
    public TimeProvider TimeProvider { get; init; } = TimeProvider.System;

    public IServiceProvider Services => _serviceProvider!;

    public async Task InitializeAsync()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:VacaFlow"] = $"Data Source={_databasePath}",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddInfrastructure(configuration);
        services.AddSingleton(TimeProvider);
        _serviceProvider = services.BuildServiceProvider();

        await using var scope = _serviceProvider.CreateAsyncScope();
        var initializer = scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>();
        await initializer.InitializeAsync(CancellationToken.None);
    }

    public async Task DisposeAsync()
    {
        if (_serviceProvider is not null)
        {
            await _serviceProvider.DisposeAsync();
        }

        // Sqlite's connection pool keeps a native file handle open even after
        // the DbContext and provider are disposed. Without this, deleting the
        // file races the pool and intermittently throws IOException.
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
