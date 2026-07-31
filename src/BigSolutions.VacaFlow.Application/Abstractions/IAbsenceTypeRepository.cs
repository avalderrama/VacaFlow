using BigSolutions.VacaFlow.Domain.AbsenceTypes;

namespace BigSolutions.VacaFlow.Application.Abstractions;

/// <summary>
/// One repository per aggregate root, exposing only what this aggregate needs
/// (CA-INF-004). No IRepository&lt;T&gt;, no IQueryable leak (CA-APP-005).
/// </summary>
public interface IAbsenceTypeRepository
{
    Task<IReadOnlyList<AbsenceType>> ListActiveAsync(CancellationToken cancellationToken);
}
