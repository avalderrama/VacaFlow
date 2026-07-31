namespace BigSolutions.VacaFlow.Api.Contracts;

/// <summary>
/// GET /api/requests/{id} response body. Mapped field by field from
/// RequestDetailDto, never the domain entity (CA-APP-006).
/// </summary>
public sealed record RequestDetailResponse(
    Guid Id,
    Guid AbsenceTypeId,
    DateOnly StartDate,
    DateOnly EndDate,
    string Reason,
    string State);
