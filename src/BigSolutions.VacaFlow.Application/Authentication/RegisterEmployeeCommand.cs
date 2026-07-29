using BigSolutions.VacaFlow.Domain.Employees;
using BigSolutions.VacaFlow.Domain.Employees.Errors;
using BigSolutions.VacaFlow.Domain.Primitives;

namespace BigSolutions.VacaFlow.Application.Authentication;

/// <summary>
/// Input to the registration use case. <see cref="Validate"/> is structural
/// validation at the application boundary (CA-APP-007, ADR-011): presence,
/// length, format. Business rules — email uniqueness, the domain's own
/// full-name invariant — stay in the domain and are checked again there,
/// deliberately, as a backstop (the same pattern the plan applies to email
/// uniqueness in the handler's step 3 and step 7).
/// </summary>
public sealed record RegisterEmployeeCommand(string? FullName, string? Email, string? Password, string? Role)
{
    private const int MinPasswordLength = 8;

    /// <summary>
    /// Bounded because the password is fed to a deliberately slow KDF (210,000
    /// PBKDF2 iterations). Without a cap, an unauthenticated caller could send a
    /// multi-megabyte password and turn the hashing cost into a CPU exhaustion
    /// vector. 128 characters is far above any real passphrase.
    /// </summary>
    private const int MaxPasswordLength = 128;

    public Result Validate()
    {
        if (string.IsNullOrWhiteSpace(FullName) || FullName.Trim().Length > 120)
        {
            return Result.Failure(EmployeeErrors.FullNameRequired);
        }

        if (string.IsNullOrWhiteSpace(Password) || Password.Length < MinPasswordLength)
        {
            return Result.Failure(EmployeeErrors.PasswordTooShort);
        }

        if (Password.Length > MaxPasswordLength)
        {
            return Result.Failure(EmployeeErrors.PasswordTooLong);
        }

        if (!TryParseRole(Role, out _))
        {
            return Result.Failure(EmployeeErrors.RoleInvalid);
        }

        return Result.Success();
    }

    public EmployeeRole ParsedRole() =>
        TryParseRole(Role, out var role) ? role : EmployeeRole.Employee;

    private static bool TryParseRole(string? role, out EmployeeRole parsed) =>
        Enum.TryParse(role, ignoreCase: true, out parsed) && Enum.IsDefined(parsed);
}
