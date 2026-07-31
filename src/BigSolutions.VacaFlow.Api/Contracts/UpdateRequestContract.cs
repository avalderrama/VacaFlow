namespace BigSolutions.VacaFlow.Api.Contracts;

/// <summary>
/// PUT /api/requests/{id} request body (FRD.md §6.3). Mirrors
/// CreateRequestContract's shape by coincidence, not by inheritance — the two
/// evolve independently as separate endpoint boundaries (CA-APP-006). Carries
/// no employeeId or requestId — the id travels in the route, the owner comes
/// exclusively from the authenticated session (FR-AUT-010).
/// </summary>
public sealed record UpdateRequestContract(Guid? AbsenceTypeId, DateOnly? StartDate, DateOnly? EndDate, string? Reason);
