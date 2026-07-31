namespace BigSolutions.VacaFlow.Api.Contracts;

/// <summary>
/// Mirror of { comment? } (FRD.md §6.3) — no responsibleManagerId: the
/// guarantee that the responsible manager is always the authenticated
/// caller (FR-DEC-006) is enforced by the contract's shape, not by a
/// check that could be forgotten.
/// </summary>
public sealed record ApproveRequestContract(string? Comment);
