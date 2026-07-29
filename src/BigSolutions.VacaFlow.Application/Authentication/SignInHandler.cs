using BigSolutions.VacaFlow.Application.Abstractions;
using BigSolutions.VacaFlow.Domain.Employees;
using BigSolutions.VacaFlow.Domain.Employees.Errors;
using BigSolutions.VacaFlow.Domain.Primitives;

namespace BigSolutions.VacaFlow.Application.Authentication;

/// <summary>
/// Verifies credentials and reports who signed in (US-008, FR-AUT-005).
/// Establishing the session itself belongs to the edge — this handler decides
/// whether the caller is who they claim, and nothing about cookies.
/// </summary>
public sealed class SignInHandler(
    IEmployeeRepository employees,
    ICredentialStore credentialStore,
    IPasswordHasher passwordHasher)
{
    /// <summary>
    /// A valid hash of a value nobody can supply. Verifying against it costs the
    /// same as verifying a real one, which is the point: see <see cref="Handle"/>.
    /// </summary>
    private const string DecoyHash =
        "pbkdf2-sha256$210000$AAAAAAAAAAAAAAAAAAAAAA==$AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";

    public async Task<Result<AuthenticatedUserDto>> Handle(
        SignInCommand command,
        CancellationToken cancellationToken)
    {
        var validation = command.Validate();
        if (validation.IsFailure)
        {
            return Result.Failure<AuthenticatedUserDto>(validation.Error);
        }

        var emailResult = Email.Create(command.Email);
        if (emailResult.IsFailure)
        {
            // A malformed address must be indistinguishable from an unknown one
            // (FR-AUT-006), so this is not reported as a validation error.
            return Result.Failure<AuthenticatedUserDto>(EmployeeErrors.InvalidCredentials);
        }

        var employee = await employees.GetByEmailAsync(emailResult.Value, cancellationToken);

        var storedHash = employee is null
            ? null
            : await credentialStore.FindHashAsync(employee.Id, cancellationToken);

        // Always run the verification, even when there is no account. Returning
        // early here would answer in microseconds while a real account costs
        // 210,000 PBKDF2 iterations, and that difference is a timing oracle for
        // account enumeration — it would undo the non-disclosure FR-AUT-006
        // requires of the response body.
        var passwordMatches = passwordHasher.Verify(command.Password!, storedHash ?? DecoyHash);

        if (employee is null || storedHash is null || !passwordMatches)
        {
            return Result.Failure<AuthenticatedUserDto>(EmployeeErrors.InvalidCredentials);
        }

        // Checked only after the password is known to be correct. The other
        // order would make this error an existence oracle: anyone could learn
        // which addresses have accounts by watching for "not active".
        if (!employee.IsActive)
        {
            return Result.Failure<AuthenticatedUserDto>(EmployeeErrors.AccountInactive);
        }

        return Result.Success(new AuthenticatedUserDto(
            employee.Id.Value,
            employee.FullName,
            employee.Email.Value,
            employee.Role.ToString()));
    }
}
