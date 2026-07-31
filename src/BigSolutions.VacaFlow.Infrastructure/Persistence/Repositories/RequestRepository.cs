using BigSolutions.VacaFlow.Application.Abstractions;
using BigSolutions.VacaFlow.Domain.Requests;

namespace BigSolutions.VacaFlow.Infrastructure.Persistence.Repositories;

internal sealed class RequestRepository(VacaFlowDbContext dbContext) : IRequestRepository
{
    public void Add(Request request) => dbContext.Requests.Add(request);
}
