namespace BigSolutions.VacaFlow.Api.Contracts;

/// <summary>
/// POST /api/requests request body (FRD.md §6.3). Carries no employeeId or any
/// other identity value — the owner comes exclusively from the authenticated
/// session (FR-AUT-010, AC4); an injected employeeId is discarded by JSON
/// binding since this contract has no such property.
/// </summary>
public sealed record CreateRequestContract(Guid? AbsenceTypeId, DateOnly? StartDate, DateOnly? EndDate, string? Reason);
