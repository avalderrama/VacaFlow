namespace BigSolutions.VacaFlow.Api.Contracts;

/// <summary>
/// POST /api/auth/login request body. Carries no identity value beyond the
/// credentials themselves — TC-08 holds by the shape of this contract.
/// </summary>
public sealed record SignInContract(string Email, string Password);
