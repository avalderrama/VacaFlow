using BigSolutions.VacaFlow.Application.Abstractions;
using BigSolutions.VacaFlow.Domain.Requests;

namespace BigSolutions.VacaFlow.Application.UnitTests.Requests.Fakes;

internal sealed class FakeRequestRepository(params Request[] seeded) : IRequestRepository
{
    private readonly List<Request> _store = [.. seeded];
    private readonly List<Request> _added = [];

    public IReadOnlyList<Request> Added => _added;

    public Task<Request?> GetByIdAsync(RequestId id, CancellationToken cancellationToken) =>
        Task.FromResult(_store.FirstOrDefault(request => request.Id == id));

    public void Add(Request request)
    {
        _store.Add(request);
        _added.Add(request);
    }
}
