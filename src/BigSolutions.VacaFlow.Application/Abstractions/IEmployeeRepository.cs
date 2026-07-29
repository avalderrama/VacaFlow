using BigSolutions.VacaFlow.Domain.Employees;

namespace BigSolutions.VacaFlow.Application.Abstractions;

/// <summary>
/// One repository per aggregate root, exposing only what this aggregate needs
/// (CA-INF-004). No IRepository&lt;T&gt;, no IQueryable leak (CA-APP-005).
/// </summary>
public interface IEmployeeRepository
{
    Task<bool> EmailExistsAsync(Email email, CancellationToken cancellationToken);

    Task<Employee?> GetByEmailAsync(Email email, CancellationToken cancellationToken);

    void Add(Employee employee);
}
