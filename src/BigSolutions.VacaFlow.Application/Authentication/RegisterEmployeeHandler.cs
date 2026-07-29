using BigSolutions.VacaFlow.Application.Abstractions;
using BigSolutions.VacaFlow.Domain.Employees;
using BigSolutions.VacaFlow.Domain.Employees.Errors;
using BigSolutions.VacaFlow.Domain.Primitives;

namespace BigSolutions.VacaFlow.Application.Authentication;

/// <summary>
/// Registers a new employee and their credentials (US-007, FR-AUT-001).
/// Ownership of the acting identity has no meaning yet for this use case —
/// there is no authenticated caller at registration time — so, unlike every
/// other handler in this codebase, it takes no dependency on ICurrentUser.
/// </summary>
public sealed class RegisterEmployeeHandler(
    IEmployeeRepository employees,
    ICredentialStore credentialStore,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork,
    IIdGenerator idGenerator)
{
    public async Task<Result<RegisteredAccountDto>> Handle(
        RegisterEmployeeCommand command,
        CancellationToken cancellationToken)
    {
        var validation = command.Validate();
        if (validation.IsFailure)
        {
            return Result.Failure<RegisteredAccountDto>(validation.Error);
        }

        var emailResult = Email.Create(command.Email);
        if (emailResult.IsFailure)
        {
            return Result.Failure<RegisteredAccountDto>(emailResult.Error);
        }

        var email = emailResult.Value;

        // Gives the correct message in the common case. The unit-of-work
        // translation in the final step closes the race window between this
        // check and the insert — both checks are deliberate (plan §3.1).
        if (await employees.EmailExistsAsync(email, cancellationToken))
        {
            return Result.Failure<RegisteredAccountDto>(EmployeeErrors.EmailAlreadyRegistered);
        }

        var employeeResult = Employee.Create(
            new EmployeeId(idGenerator.NewId()),
            command.FullName,
            email,
            command.ParsedRole());

        if (employeeResult.IsFailure)
        {
            return Result.Failure<RegisteredAccountDto>(employeeResult.Error);
        }

        var employee = employeeResult.Value;
        var passwordHash = passwordHasher.Hash(command.Password!);

        employees.Add(employee);
        credentialStore.Add(employee.Id, passwordHash);

        var saveResult = await unitOfWork.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
        {
            return Result.Failure<RegisteredAccountDto>(saveResult.Error);
        }

        return Result.Success(new RegisteredAccountDto(
            employee.Id.Value,
            employee.FullName,
            employee.Email.Value,
            employee.Role.ToString()));
    }
}
