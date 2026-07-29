using BigSolutions.VacaFlow.Domain.Primitives;

namespace BigSolutions.VacaFlow.Application.Abstractions;

/// <summary>
/// Marks the transaction boundary a use case decides, without knowing the
/// concrete mechanism (CA-APP-008). Returns a Result rather than throwing, so
/// a constraint violation the repository check missed (a race between the
/// check and the insert) is still an expected outcome, not an exception.
/// </summary>
public interface IUnitOfWork
{
    Task<Result> SaveChangesAsync(CancellationToken cancellationToken);
}
