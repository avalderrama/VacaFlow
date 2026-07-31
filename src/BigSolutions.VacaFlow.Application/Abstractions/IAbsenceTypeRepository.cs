using BigSolutions.VacaFlow.Domain.AbsenceTypes;

namespace BigSolutions.VacaFlow.Application.Abstractions;

/// <summary>
/// One repository per aggregate root, exposing only what this aggregate needs
/// (CA-INF-004). No IRepository&lt;T&gt;, no IQueryable leak (CA-APP-005).
/// </summary>
public interface IAbsenceTypeRepository
{
    Task<IReadOnlyList<AbsenceType>> ListActiveAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Whether an active absence type with this id exists, without loading the
    /// full aggregate (FR-CAT-003, US-015 plan D6).
    /// </summary>
    Task<bool> ExistsActiveAsync(AbsenceTypeId id, CancellationToken cancellationToken);

    /// <summary>
    /// Batch resolution by id, for enriching a list without N+1 (US-020).
    /// Unlike ListActiveAsync, this includes inactive types: a historical
    /// request whose absence type was later deactivated must still show its
    /// code/name.
    /// </summary>
    Task<IReadOnlyList<AbsenceType>> ListByIdsAsync(IReadOnlyCollection<AbsenceTypeId> ids, CancellationToken cancellationToken);
}
