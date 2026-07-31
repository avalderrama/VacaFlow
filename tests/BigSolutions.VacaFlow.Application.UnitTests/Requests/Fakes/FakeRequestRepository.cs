using BigSolutions.VacaFlow.Application.Abstractions;
using BigSolutions.VacaFlow.Domain.Employees;
using BigSolutions.VacaFlow.Domain.Requests;

namespace BigSolutions.VacaFlow.Application.UnitTests.Requests.Fakes;

internal sealed class FakeRequestRepository(params Request[] seeded) : IRequestRepository
{
    private readonly List<Request> _store = [.. seeded];
    private readonly List<Request> _added = [];
    private readonly Dictionary<EmployeeId, EmployeeId> _managerByOwner = [];

    public IReadOnlyList<Request> Added => _added;

    /// <summary>Replicates Employee.ManagerId for ListPendingForManagerAsync's join (US-020).</summary>
    public FakeRequestRepository WithManagerAssignment(EmployeeId owner, EmployeeId manager)
    {
        _managerByOwner[owner] = manager;
        return this;
    }

    public Task<Request?> GetByIdAsync(RequestId id, CancellationToken cancellationToken) =>
        Task.FromResult(_store.FirstOrDefault(request => request.Id == id));

    public void Add(Request request)
    {
        _store.Add(request);
        _added.Add(request);
    }

    public Task<IReadOnlyList<Request>> ListOwnedByAsync(EmployeeId owner, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Request>>(_store
            .Where(request => request.OwnerId == owner)
            .OrderByDescending(request => request.CreatedAtUtc)
            .ToList());

    public Task<IReadOnlyList<Request>> ListPendingForManagerAsync(EmployeeId manager, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Request>>(_store
            .Where(request =>
                request.State == RequestState.Submitted &&
                request.OwnerId != manager &&
                _managerByOwner.TryGetValue(request.OwnerId, out var assignedManager) &&
                assignedManager == manager)
            .OrderByDescending(request => request.CreatedAtUtc)
            .ToList());
}
