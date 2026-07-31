using BigSolutions.VacaFlow.Application.Abstractions;
using BigSolutions.VacaFlow.Domain.AbsenceTypes;
using Microsoft.EntityFrameworkCore;

namespace BigSolutions.VacaFlow.Infrastructure.Persistence.Repositories;

internal sealed class AbsenceTypeRepository(VacaFlowDbContext dbContext) : IAbsenceTypeRepository
{
    public async Task<IReadOnlyList<AbsenceType>> ListActiveAsync(CancellationToken cancellationToken) =>
        await dbContext.AbsenceTypes
            .AsNoTracking()
            .Where(type => type.IsActive)
            .OrderBy(type => type.Name)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsActiveAsync(AbsenceTypeId id, CancellationToken cancellationToken) =>
        dbContext.AbsenceTypes.AnyAsync(type => type.Id == id && type.IsActive, cancellationToken);
}
