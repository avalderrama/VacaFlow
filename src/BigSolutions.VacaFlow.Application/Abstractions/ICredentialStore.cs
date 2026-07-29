using BigSolutions.VacaFlow.Domain.Employees;

namespace BigSolutions.VacaFlow.Application.Abstractions;

/// <summary>
/// Persists the password hash for an employee. Kept as its own port, distinct
/// from IEmployeeRepository, so the UserAccount technical table (Intent.md
/// §7.1) can stay internal to Infrastructure and Application never sees it.
/// Not anticipated by SAD.md §6.3; added by US-007.
/// </summary>
public interface ICredentialStore
{
    void Add(EmployeeId employeeId, string passwordHash);

    Task<string?> FindHashAsync(EmployeeId employeeId, CancellationToken cancellationToken);
}
