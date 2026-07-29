namespace BigSolutions.VacaFlow.Api.Contracts;

/// <summary>
/// Response body for POST /api/auth/register and POST /api/auth/login
/// (FRD.md §6.1), and for GET /auth/me when US-010 lands. Mapped field by field
/// from AuthenticatedUserDto — never the domain entity, never the password hash
/// (CA-APP-006, NFR-SEC-002).
/// </summary>
public sealed record AuthenticatedUserResponse(Guid Id, string FullName, string Email, string Role);
