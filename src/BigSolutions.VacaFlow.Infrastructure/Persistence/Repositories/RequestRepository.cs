using BigSolutions.VacaFlow.Application.Abstractions;
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
}
