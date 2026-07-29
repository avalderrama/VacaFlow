using BigSolutions.VacaFlow.Application.Abstractions;
using BigSolutions.VacaFlow.Domain.Employees;

namespace BigSolutions.VacaFlow.Application.UnitTests.Authentication.Fakes;

/// <summary>Hand-written test double (CA-TST-003) — no mocking framework is a project dependency.</summary>
internal sealed class FakeEmployeeRepository : IEmployeeRepository
{
    private readonly HashSet<string> _existingEmails;

    public FakeEmployeeRepository(params string[] existingEmails) =>
        _existingEmails = new HashSet<string>(existingEmails, StringComparer.Ordinal);

    public List<Employee> AddedEmployees { get; } = [];

    public Task<bool> EmailExistsAsync(Email email, CancellationToken cancellationToken) =>
        Task.FromResult(_existingEmails.Contains(email.Value));

    public void Add(Employee employee) => AddedEmployees.Add(employee);
}
