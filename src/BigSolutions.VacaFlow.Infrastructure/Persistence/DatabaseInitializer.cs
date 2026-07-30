using BigSolutions.VacaFlow.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace BigSolutions.VacaFlow.Infrastructure.Persistence;

/// <summary>
/// Applies pending migrations and seeds fixed demo data at startup
/// (FR-DAT-001, TE-003). Exists as its own port because VacaFlowDbContext is
/// internal and the composition root cannot reach it any other way without
/// breaking CA-DEP-007 (SAD.md §6.3 delta). Seeding after migrating, not as a
/// separate port, keeps "prepare the database" a single startup step — any
/// test fixture that calls this (SqliteDatabaseFixture today) ends up with a
/// database seeded the same way a real one would.
/// </summary>
internal sealed class DatabaseInitializer(VacaFlowDbContext dbContext, DatabaseSeeder seeder) : IDatabaseInitializer
{
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await dbContext.Database.MigrateAsync(cancellationToken);
        await seeder.SeedAsync(cancellationToken);
    }
}
