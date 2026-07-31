namespace BigSolutions.VacaFlow.Application.Requests;

/// <summary>
/// Read model for a single request, owner-only (US-017, RULE-04). State
/// travels as its enum name — the same labels §3.4 uses ("Draft",
/// "Submitted", ...) — so the client needs no translation table.
/// </summary>
public sealed record RequestDetailDto(
    Guid Id,
    Guid AbsenceTypeId,
    DateOnly StartDate,
    DateOnly EndDate,
    string Reason,
    string State);
