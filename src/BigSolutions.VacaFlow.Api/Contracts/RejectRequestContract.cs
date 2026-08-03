namespace BigSolutions.VacaFlow.Api.Contracts;

/// <summary>
/// Mirror of { comment? } (FRD.md §6.3) — no responsibleManagerId: the
/// guarantee that the responsible manager is always the authenticated
/// caller (FR-DEC-006) is enforced by the contract's shape, not by a
/// check that could be forgotten. Same shape as ApproveRequestContract
/// (FRD.md §6.3 defines one shared contract block for both verbs); kept
/// as a distinct type per the one-contract-per-operation convention.
/// </summary>
public sealed record RejectRequestContract(string? Comment);
