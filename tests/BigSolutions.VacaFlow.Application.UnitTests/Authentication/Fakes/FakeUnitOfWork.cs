using BigSolutions.VacaFlow.Application.Abstractions;
using BigSolutions.VacaFlow.Domain.Primitives;

namespace BigSolutions.VacaFlow.Application.UnitTests.Authentication.Fakes;

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    private readonly Result _resultToReturn;

    public FakeUnitOfWork(Result? resultToReturn = null) => _resultToReturn = resultToReturn ?? Result.Success();

    public int SaveChangesCallCount { get; private set; }

    public Task<Result> SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveChangesCallCount++;
        return Task.FromResult(_resultToReturn);
    }
}
