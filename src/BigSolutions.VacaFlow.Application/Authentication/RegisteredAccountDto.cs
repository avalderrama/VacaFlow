namespace BigSolutions.VacaFlow.Application.Authentication;

/// <summary>
/// Output of the registration use case (CA-APP-006). A domain entity is never
/// returned across the boundary — this carries exactly what the API needs to
/// build its response, and nothing else. In particular, no password hash.
/// </summary>
public sealed record RegisteredAccountDto(Guid Id, string FullName, string Email, string Role);
