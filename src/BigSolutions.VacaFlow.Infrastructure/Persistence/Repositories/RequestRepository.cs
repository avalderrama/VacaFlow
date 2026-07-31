using BigSolutions.VacaFlow.Application.Abstractions;
using BigSolutions.VacaFlow.Domain.Employees;
using BigSolutions.VacaFlow.Domain.Requests;
using Microsoft.EntityFrameworkCore;

namespace BigSolutions.VacaFlow.Infrastructure.Persistence.Repositories;

internal sealed class RequestRepository(VacaFlowDbContext dbContext) : IRequestRepository
{
    /// <remarks>
    /// Returns a tracked entity — the caller mutates it in place and
    /// SaveChangesAsync persists the change through EF's own change tracking,
    /// no explicit Update() call needed (same pattern CredentialStore relies
    /// on).
    /// </remarks>
    public Task<Request?> GetByIdAsync(RequestId id, CancellationToken cancellationToken) =>
        dbContext.Requests.FirstOrDefaultAsync(request => request.Id == id, cancellationToken);

    public void Add(Request request) => dbContext.Requests.Add(request);

    /// <remarks>
    /// AsNoTracking — unlike GetByIdAsync, nothing here is ever mutated.
    /// </remarks>
    public async Task<IReadOnlyList<Request>> ListOwnedByAsync(EmployeeId owner, CancellationToken cancellationToken) =>
        await dbContext.Requests
            .AsNoTracking()
            .Where(request => request.OwnerId == owner)
            .OrderByDescending(request => request.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    /// <remarks>
    /// The Requests-to-Employees join lives here, repo-side (CA-APP-005) —
    /// the port exposes intent, not composition. "request.OwnerId != manager"
    /// is a belt-and-suspenders check on top of the join: Employee.AssignManager
    /// already makes ManagerId == Id structurally impossible, but this makes
    /// AC4's "never" guarantee defensive rather than merely structural.
    /// </remarks>
    public async Task<IReadOnlyList<Request>> ListPendingForManagerAsync(EmployeeId manager, CancellationToken cancellationToken) =>
        await dbContext.Requests
            .AsNoTracking()
            .Where(request =>
                request.State == RequestState.Submitted &&
                request.OwnerId != manager &&
                dbContext.Employees.Any(employee => employee.Id == request.OwnerId && employee.ManagerId == manager))
            .OrderByDescending(request => request.CreatedAtUtc)
            .ToListAsync(cancellationToken);
}
