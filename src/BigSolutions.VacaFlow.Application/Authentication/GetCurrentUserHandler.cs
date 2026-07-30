using BigSolutions.VacaFlow.Application.Abstractions;
using BigSolutions.VacaFlow.Domain.Employees.Errors;
using BigSolutions.VacaFlow.Domain.Primitives;

namespace BigSolutions.VacaFlow.Application.Authentication;

/// <summary>
/// Answers "who am I" for the session's own owner (US-010, FR-AUT-009). The
/// acting identity comes entirely from <see cref="ICurrentUser"/> — there is
/// no input to accept or validate, so there is no command or query record.
/// </summary>
public sealed class GetCurrentUserHandler(ICurrentUser currentUser, IEmployeeRepository employees)
{
    public async Task<Result<AuthenticatedUserDto>> Handle(CancellationToken cancellationToken)
    {
        var employee = await employees.GetByIdAsync(currentUser.EmployeeId, cancellationToken);

        // The cookie is valid (the FallbackPolicy already guarantees that),
        // but the employee it names is gone — a data lifecycle mismatch, not
        // a bug. "You must be signed in" is literally true: that identity no
        // longer identifies anyone.
        if (employee is null)
        {
            return Result.Failure<AuthenticatedUserDto>(EmployeeErrors.NotAuthenticated);
        }

        return Result.Success(new AuthenticatedUserDto(
            employee.Id.Value,
            employee.FullName,
            employee.Email.Value,
            employee.Role.ToString()));
    }
}
