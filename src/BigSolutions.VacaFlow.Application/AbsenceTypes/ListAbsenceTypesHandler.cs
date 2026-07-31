using BigSolutions.VacaFlow.Application.Abstractions;

namespace BigSolutions.VacaFlow.Application.AbsenceTypes;

/// <summary>
/// Lists the active absence types for the request form's selector (US-014).
/// No command/query record: there is nothing for a client to supply. No
/// Result&lt;T&gt;: a plain catalog read has no business failure to report — the
/// coarse "must be signed in" gate is the endpoint's job, not this handler's.
/// </summary>
public sealed class ListAbsenceTypesHandler(IAbsenceTypeRepository absenceTypes)
{
    public async Task<IReadOnlyList<AbsenceTypeDto>> Handle(CancellationToken cancellationToken)
    {
        var types = await absenceTypes.ListActiveAsync(cancellationToken);

        return types
            .Select(type => new AbsenceTypeDto(type.Id.Value, type.Code.Value, type.Name))
            .ToList();
    }
}
