using BigSolutions.VacaFlow.Application.Abstractions;
using BigSolutions.VacaFlow.Domain.Employees;

namespace BigSolutions.VacaFlow.Application.UnitTests.Authentication.Fakes;

internal sealed class FakeCurrentUser(EmployeeId employeeId, EmployeeRole role = EmployeeRole.Employee)
    : ICurrentUser
{
    public EmployeeId EmployeeId { get; } = employeeId;

    public EmployeeRole Role { get; } = role;
}
