using BigSolutions.VacaFlow.Domain.Employees;
using Microsoft.EntityFrameworkCore;

namespace BigSolutions.VacaFlow.Infrastructure.Persistence;

/// <summary>
/// Internal (CA-DEP-007) — the API physically cannot construct a repository
/// or reach this context directly, which is what makes CA-DEP-008 a
/// compile-time guarantee rather than a convention (SAD.md §7.1).
/// </summary>
internal sealed class VacaFlowDbContext(DbContextOptions<VacaFlowDbContext> options)
    : DbContext(options)
{
    public DbSet<Employee> Employees => Set<Employee>();

    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VacaFlowDbContext).Assembly);
    }
}
