using BigSolutions.VacaFlow.Application.Abstractions;
using BigSolutions.VacaFlow.Domain.Primitives;
using BigSolutions.VacaFlow.Domain.Requests;
using BigSolutions.VacaFlow.Domain.Requests.Errors;

namespace BigSolutions.VacaFlow.Application.Requests;

/// <summary>
/// Cancels an owner's own request (US-019). No input beyond the route id —
/// same shape as SubmitRequestHandler (ADR-011 governs commands with a
/// body, not this). Ownership (RULE-04) is checked before the transition
/// itself: a manager has no special standing here either — nobody but the
/// request's own owner may cancel it.
/// </summary>
public sealed class CancelRequestHandler(
    ICurrentUser currentUser,
    IRequestRepository requests,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<Result> Handle(Guid requestId, CancellationToken cancellationToken)
    {
        var request = await requests.GetByIdAsync(new RequestId(requestId), cancellationToken);
        if (request is null)
        {
            return Result.Failure(RequestErrors.NotFound);
        }

        if (request.OwnerId != currentUser.EmployeeId)
        {
            return Result.Failure(RequestErrors.NotOwner);
        }

        var cancelResult = request.Cancel(timeProvider.GetUtcNow().UtcDateTime);
        if (cancelResult.IsFailure)
        {
            return cancelResult;
        }

        return await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
