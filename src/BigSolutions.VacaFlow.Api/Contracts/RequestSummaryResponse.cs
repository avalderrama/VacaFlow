namespace BigSolutions.VacaFlow.Api.Contracts;

/// <remarks>
/// A row of GET /api/requests (FRD.md §6.3) — no Approval block yet
/// (US-020 plan D8). For a Manager, this endpoint's overall response is
/// the union of their own requests (any state) plus their team's
/// Submitted ones (FR-VIS-001, US-020 plan D3) — by design, not a bug.
/// There is no discriminator field: a consumer deriving "the approval
/// queue" (US-023) must exclude rows where Employee.Id equals the
/// caller's own id (from GET /auth/me), or it will render the manager's
/// own request alongside their team's.
/// </remarks>
public sealed record RequestSummaryResponse(
    Guid Id,
    AbsenceTypeSummaryResponse AbsenceType,
    DateOnly StartDate,
    DateOnly EndDate,
    string Reason,
    string State,
    EmployeeSummaryResponse Employee,
    DateTime CreatedAtUtc);

/// <remarks>
/// Field-for-field identical to Contracts.AbsenceTypeResponse — kept
/// separate so this response stays self-contained (same reasoning as
/// Application.Requests.AbsenceTypeSummaryDto, which this type mirrors).
/// </remarks>
public sealed record AbsenceTypeSummaryResponse(Guid Id, string Code, string Name);

public sealed record EmployeeSummaryResponse(Guid Id, string FullName);
