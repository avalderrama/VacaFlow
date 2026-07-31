using BigSolutions.VacaFlow.Domain.Requests;

namespace BigSolutions.VacaFlow.Application.Abstractions;

/// <summary>
/// One repository per aggregate root, exposing only what this aggregate needs
/// (CA-INF-004). No IRepository&lt;T&gt;, no IQueryable leak (CA-APP-005).
/// Listing operations arrive with their first consumer (US-018/US-020,
/// US-015 plan D8).
/// </summary>
public interface IRequestRepository
{
    Task<Request?> GetByIdAsync(RequestId id, CancellationToken cancellationToken);

    void Add(Request request);
}
