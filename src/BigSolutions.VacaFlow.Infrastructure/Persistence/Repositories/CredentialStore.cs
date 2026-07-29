using BigSolutions.VacaFlow.Application.Abstractions;
using BigSolutions.VacaFlow.Domain.Employees;
using Microsoft.EntityFrameworkCore;

namespace BigSolutions.VacaFlow.Infrastructure.Persistence.Repositories;

/// <summary>
/// The only type that knows UserAccount exists (SAD.md §7.2). CreatedAtUtc is
/// stamped here from the injected clock rather than passed in by the
/// application layer, which has no other use for it in this use case
/// (CA-DOM-009, CA-CRS-002).
/// </summary>
internal sealed class CredentialStore(VacaFlowDbContext dbContext, TimeProvider timeProvider) : ICredentialStore
{
    public void Add(EmployeeId employeeId, string passwordHash)
    {
        dbContext.UserAccounts.Add(new UserAccount
        {
            Id = Guid.CreateVersion7(),
            EmployeeId = employeeId,
            PasswordHash = passwordHash,
            CreatedAtUtc = timeProvider.GetUtcNow().UtcDateTime,
        });
    }

    public async Task<string?> FindHashAsync(EmployeeId employeeId, CancellationToken cancellationToken)
    {
        var account = await dbContext.UserAccounts
            .FirstOrDefaultAsync(userAccount => userAccount.EmployeeId == employeeId, cancellationToken);

        return account?.PasswordHash;
    }
}
