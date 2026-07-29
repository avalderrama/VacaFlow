namespace BigSolutions.VacaFlow.Api.Contracts;

/// <summary>
/// POST /api/auth/register request body. Carries no employeeId or any other
/// identity value — TC-08 holds by the shape of this contract, not by a
/// runtime check.
/// </summary>
public sealed record RegisterAccountContract(string FullName, string Email, string Password, string Role);
