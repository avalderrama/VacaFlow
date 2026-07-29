namespace BigSolutions.VacaFlow.Application.Abstractions;

/// <summary>
/// Applies pending migrations at startup. Exists because VacaFlowDbContext is
/// internal to Infrastructure (CA-DEP-007) — the composition root cannot
/// reference it directly, and resolving it via the container from inside
/// Infrastructure would violate CA-CFG-003. Not anticipated by SAD.md §6.3;
/// added by US-007.
/// </summary>
public interface IDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken);
}
