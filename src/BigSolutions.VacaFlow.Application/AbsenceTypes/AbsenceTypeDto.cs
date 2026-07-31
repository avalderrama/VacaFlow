namespace BigSolutions.VacaFlow.Application.AbsenceTypes;

/// <summary>
/// One entry of the seeded absence type catalog, as the query reports it
/// (CA-APP-006). A domain entity is never returned across the boundary.
/// </summary>
public sealed record AbsenceTypeDto(Guid Id, string Code, string Name);
