using BigSolutions.VacaFlow.Application.Abstractions;
using BigSolutions.VacaFlow.Domain.Employees;
using Microsoft.EntityFrameworkCore;

namespace BigSolutions.VacaFlow.Infrastructure.Persistence.Repositories;

internal sealed class EmployeeRepository(VacaFlowDbContext dbContext) : IEmployeeRepository
{
    /// <remarks>
    /// EF Core translates this comparison through the Email ValueConverter and
    /// ignores the operator== that ValueObject defines — the SQL compares the
    /// stored string, not the value object. The two agree today only because
    /// Email's single atomic value is exactly what the converter writes.
    /// Nothing in the compiler enforces that agreement, so
    /// EmployeeRepositoryTests is load-bearing: if Email ever gains a second
    /// atomic value, this line keeps compiling and keeps emitting the same SQL
    /// while silently meaning something different.
    /// </remarks>
    public Task<bool> EmailExistsAsync(Email email, CancellationToken cancellationToken) =>
        dbContext.Employees.AnyAsync(employee => employee.Email == email, cancellationToken);

    public void Add(Employee employee) => dbContext.Employees.Add(employee);
}
