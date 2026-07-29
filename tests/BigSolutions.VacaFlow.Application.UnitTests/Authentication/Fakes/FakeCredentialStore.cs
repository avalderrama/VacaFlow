using BigSolutions.VacaFlow.Application.Abstractions;
using BigSolutions.VacaFlow.Domain.Employees;

namespace BigSolutions.VacaFlow.Application.UnitTests.Authentication.Fakes;

internal sealed class FakeCredentialStore : ICredentialStore
{
    private readonly Dictionary<EmployeeId, string> _hashesByEmployeeId = [];

    public void Add(EmployeeId employeeId, string passwordHash) => _hashesByEmployeeId[employeeId] = passwordHash;

    public Task<string?> FindHashAsync(EmployeeId employeeId, CancellationToken cancellationToken) =>
        Task.FromResult(_hashesByEmployeeId.GetValueOrDefault(employeeId));
}
