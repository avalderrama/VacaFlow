using BigSolutions.VacaFlow.Application.Abstractions;
using BigSolutions.VacaFlow.Domain.Requests;

namespace BigSolutions.VacaFlow.Application.UnitTests.Requests.Fakes;

internal sealed class FakeRequestRepository : IRequestRepository
{
    private readonly List<Request> _added = [];

    public IReadOnlyList<Request> Added => _added;

    public void Add(Request request) => _added.Add(request);
}
