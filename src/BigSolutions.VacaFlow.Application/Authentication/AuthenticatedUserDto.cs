namespace BigSolutions.VacaFlow.Application.Authentication;

/// <summary>
/// Who the caller is, as the authentication use cases report it (CA-APP-006).
/// Shared by registration, sign-in and GET /auth/me — all three answer the
/// same question, and three identical records would be duplication rather
/// than boundary clarity.
/// </summary>
/// <remarks>
/// A domain entity is never returned across the boundary, and there is no field
/// here that could carry a password hash (NFR-SEC-002).
/// </remarks>
public sealed record AuthenticatedUserDto(Guid Id, string FullName, string Email, string Role);
