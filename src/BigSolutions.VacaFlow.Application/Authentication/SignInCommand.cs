using BigSolutions.VacaFlow.Domain.Employees.Errors;
using BigSolutions.VacaFlow.Domain.Primitives;

namespace BigSolutions.VacaFlow.Application.Authentication;

/// <summary>
/// Input to the sign-in use case.
/// </summary>
/// <remarks>
/// Validation checks presence only. It deliberately does not apply the
/// registration password policy: enforcing a minimum length here would reveal
/// that policy to an unauthenticated caller, and would lock out any account
/// created before the policy changed. Whether the password is right is decided
/// by the hash, not by its shape.
///
/// A failure here returns InvalidCredentials rather than a field-level
/// validation error, for the same reason — see FR-AUT-006.
/// </remarks>
public sealed record SignInCommand(string? Email, string? Password)
{
    public Result Validate() =>
        string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password)
            ? Result.Failure(EmployeeErrors.InvalidCredentials)
            : Result.Success();
}
