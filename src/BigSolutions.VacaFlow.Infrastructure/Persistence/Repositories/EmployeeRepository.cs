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

    /// <remarks>
    /// Same EF Core translation caveat as <see cref="EmailExistsAsync"/> above.
    /// </remarks>
    public Task<Employee?> GetByEmailAsync(Email email, CancellationToken cancellationToken) =>
        dbContext.Employees.FirstOrDefaultAsync(employee => employee.Email == email, cancellationToken);

    /// <remarks>
    /// Same converter-mediated comparison as <see cref="EmailExistsAsync"/>,
    /// with lower risk: EmployeeId wraps a single Guid with no plausible
    /// second atomic value and is never regenerated, so the equality the
    /// converter's SQL expresses cannot drift from EmployeeId's own
    /// operator== the way Email's could.
    /// </remarks>
    public Task<Employee?> GetByIdAsync(EmployeeId id, CancellationToken cancellationToken) =>
        dbContext.Employees.FirstOrDefaultAsync(employee => employee.Id == id, cancellationToken);

    public void Add(Employee employee) => dbContext.Employees.Add(employee);
}
