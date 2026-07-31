using BigSolutions.VacaFlow.Domain.Employees;
using BigSolutions.VacaFlow.Domain.Requests;

namespace BigSolutions.VacaFlow.Application.Abstractions;

/// <summary>
/// One repository per aggregate root, exposing only what this aggregate needs
/// (CA-INF-004). No IRepository&lt;T&gt;, no IQueryable leak (CA-APP-005).
/// Listing operations arrived with their first consumer (US-020).
/// </summary>
public interface IRequestRepository
{
    Task<Request?> GetByIdAsync(RequestId id, CancellationToken cancellationToken);

    void Add(Request request);

    /// <summary>All of the owner's own requests, every state, most recent first (FR-VIS-001).</summary>
    Task<IReadOnlyList<Request>> ListOwnedByAsync(EmployeeId owner, CancellationToken cancellationToken);

    /// <summary>
    /// Only the Submitted requests of employees assigned to this manager
    /// (Employee.ManagerId == manager), excluding the manager's own requests
    /// even if they were assigned to themselves, most recent first
    /// (FR-VIS-001, FR-VIS-002, FR-VIS-003).
    /// </summary>
    Task<IReadOnlyList<Request>> ListPendingForManagerAsync(EmployeeId manager, CancellationToken cancellationToken);
}
