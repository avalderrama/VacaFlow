using BigSolutions.VacaFlow.Application.Abstractions;
using BigSolutions.VacaFlow.Domain.AbsenceTypes;

namespace BigSolutions.VacaFlow.Application.UnitTests.AbsenceTypes.Fakes;

/// <summary>Hand-written test double (CA-TST-003) — no mocking framework is a project dependency.</summary>
internal sealed class FakeAbsenceTypeRepository(params AbsenceType[] types) : IAbsenceTypeRepository
{
    public Task<IReadOnlyList<AbsenceType>> ListActiveAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<AbsenceType>>(types.Where(type => type.IsActive).ToList());

    public Task<bool> ExistsActiveAsync(AbsenceTypeId id, CancellationToken cancellationToken) =>
        Task.FromResult(types.Any(type => type.Id == id && type.IsActive));

    public Task<IReadOnlyList<AbsenceType>> ListByIdsAsync(IReadOnlyCollection<AbsenceTypeId> ids, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<AbsenceType>>(types.Where(type => ids.Contains(type.Id)).ToList());
}
