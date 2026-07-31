using BigSolutions.VacaFlow.Application.Abstractions;
using BigSolutions.VacaFlow.Domain.AbsenceTypes;

namespace BigSolutions.VacaFlow.Application.UnitTests.AbsenceTypes.Fakes;

/// <summary>Hand-written test double (CA-TST-003) — no mocking framework is a project dependency.</summary>
internal sealed class FakeAbsenceTypeRepository(params AbsenceType[] activeTypes) : IAbsenceTypeRepository
{
    public Task<IReadOnlyList<AbsenceType>> ListActiveAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<AbsenceType>>(activeTypes);
}
