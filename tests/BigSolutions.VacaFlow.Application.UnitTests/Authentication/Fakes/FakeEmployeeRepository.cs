using BigSolutions.VacaFlow.Application.Abstractions;
using BigSolutions.VacaFlow.Domain.Employees;

namespace BigSolutions.VacaFlow.Application.UnitTests.Authentication.Fakes;

/// <summary>Hand-written test double (CA-TST-003) — no mocking framework is a project dependency.</summary>
internal sealed class FakeEmployeeRepository : IEmployeeRepository
{
    private readonly HashSet<string> _existingEmails;
    private readonly Dictionary<string, Employee> _employeesByEmail = [];

    public FakeEmployeeRepository(params string[] existingEmails) =>
        _existingEmails = new HashSet<string>(existingEmails, StringComparer.Ordinal);

    public List<Employee> AddedEmployees { get; } = [];

    /// <summary>
    /// Seeds an employee that <see cref="GetByEmailAsync"/> and
    /// <see cref="GetByIdAsync"/> will return.
    /// </summary>
    public FakeEmployeeRepository WithEmployee(Employee employee)
    {
        _employeesByEmail[employee.Email.Value] = employee;
        _existingEmails.Add(employee.Email.Value);
        return this;
    }

    public Task<bool> EmailExistsAsync(Email email, CancellationToken cancellationToken) =>
        Task.FromResult(_existingEmails.Contains(email.Value));

    public Task<Employee?> GetByEmailAsync(Email email, CancellationToken cancellationToken) =>
        Task.FromResult(_employeesByEmail.GetValueOrDefault(email.Value));

    public Task<Employee?> GetByIdAsync(EmployeeId id, CancellationToken cancellationToken) =>
        Task.FromResult(_employeesByEmail.Values.FirstOrDefault(employee => employee.Id == id));

    public void Add(Employee employee) => AddedEmployees.Add(employee);

    public Task<IReadOnlyList<Employee>> ListByIdsAsync(IReadOnlyCollection<EmployeeId> ids, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Employee>>(_employeesByEmail.Values.Where(employee => ids.Contains(employee.Id)).ToList());
}
