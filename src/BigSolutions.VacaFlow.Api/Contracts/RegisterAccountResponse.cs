namespace BigSolutions.VacaFlow.Api.Contracts;

/// <summary>
/// POST /api/auth/register response body (FRD.md §6.1). Mapped field by field
/// from RegisteredAccountDto — never the domain entity, never the password
/// hash (CA-APP-006, NFR-SEC-002).
/// </summary>
public sealed record RegisterAccountResponse(Guid Id, string FullName, string Email, string Role);
