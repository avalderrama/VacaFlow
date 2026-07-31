using BigSolutions.VacaFlow.Application.Abstractions;
using BigSolutions.VacaFlow.Domain.Employees;
using BigSolutions.VacaFlow.Domain.Primitives;
using BigSolutions.VacaFlow.Domain.Requests;
using BigSolutions.VacaFlow.Domain.Requests.Errors;
using BigSolutions.VacaFlow.Domain.Requests.Services;

namespace BigSolutions.VacaFlow.Application.Requests;

/// <summary>
/// Decides on a request — approve or reject (US-021), shared by both per
/// SAD.md §6.1 ("approve and reject share DecideRequestHandler"): the
/// authorization, the record and the transaction are identical for both
/// (FR-DEC-005). Authorization (ApprovalPolicy) runs before the domain
/// transition, same precedence every handler in this codebase uses.
/// </summary>
public sealed class DecideRequestHandler(
    ICurrentUser currentUser,
    IRequestRepository requests,
    IEmployeeRepository employees,
    IUnitOfWork unitOfWork,
    IIdGenerator idGenerator,
    TimeProvider timeProvider)
{
    public async Task<Result> Handle(DecideRequestCommand command, CancellationToken cancellationToken)
    {
        var validation = command.Validate();
        if (validation.IsFailure)
        {
            return validation;
        }

        var request = await requests.GetByIdAsync(new RequestId(command.RequestId), cancellationToken);
        if (request is null)
        {
            return Result.Failure(RequestErrors.NotFound);
        }

        var owner = await employees.GetByIdAsync(request.OwnerId, cancellationToken);
        if (owner is null)
        {
            return Result.Failure(RequestErrors.NotFound);
        }

        var actingManager = await employees.GetByIdAsync(currentUser.EmployeeId, cancellationToken);
        if (actingManager is null)
        {
            return Result.Failure(RequestErrors.NotFound);
        }

        var authorization = ApprovalPolicy.CanDecide(request, owner, actingManager);
        if (authorization.IsFailure)
        {
            return authorization;
        }

        var now = timeProvider.GetUtcNow();

        var decideResult = request.Decide(
            new ApprovalId(idGenerator.NewId()),
            currentUser.EmployeeId,
            command.Decision,
            command.Comment,
            now.UtcDateTime);

        if (decideResult.IsFailure)
        {
            return decideResult;
        }

        return await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
