namespace BigSolutions.VacaFlow.Api.Contracts;

/// <summary>
/// Response body for GET /api/absence-types (FRD.md §6.2). Mapped field by
/// field from AbsenceTypeDto — never the domain entity (CA-APP-006).
/// </summary>
public sealed record AbsenceTypeResponse(Guid Id, string Code, string Name);
