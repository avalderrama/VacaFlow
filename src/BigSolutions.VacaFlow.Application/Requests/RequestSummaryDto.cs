namespace BigSolutions.VacaFlow.Application.Requests;

/// <summary>
/// A row of the FRD.md §6.3 GET /requests response — never the Approval
/// block, which the Approval aggregate does not exist to populate yet
/// (US-021, US-020 plan D8).
/// </summary>
public sealed record RequestSummaryDto(
    Guid Id,
    AbsenceTypeSummaryDto AbsenceType,
    DateOnly StartDate,
    DateOnly EndDate,
    string Reason,
    string State,
    EmployeeSummaryDto Employee,
    DateTime CreatedAtUtc);

/// <remarks>
/// Field-for-field identical to AbsenceTypes.AbsenceTypeDto — kept as its
/// own type rather than reused so this read model stays self-contained
/// within Requests/ and doesn't couple this feature folder to AbsenceTypes/
/// for a shape that could legitimately diverge later (e.g. this one never
/// needs anything ListAbsenceTypesHandler's own DTO might grow).
/// </remarks>
public sealed record AbsenceTypeSummaryDto(Guid Id, string Code, string Name);

public sealed record EmployeeSummaryDto(Guid Id, string FullName);
