using BigSolutions.VacaFlow.Application.Abstractions;
using BigSolutions.VacaFlow.Domain.Primitives;
using BigSolutions.VacaFlow.Domain.Requests;
using BigSolutions.VacaFlow.Domain.Requests.Errors;

namespace BigSolutions.VacaFlow.Application.Requests;

/// <summary>
/// Submits an owner's own draft for approval (US-018). No input beyond the
/// route id — like GetRequestByIdHandler, there is no command record (ADR-011
/// governs commands with a body, not this). The owner comparison (RULE-04) is
/// checked before the transition itself, so a non-owner never learns the
/// request's current state.
/// </summary>
public sealed class SubmitRequestHandler(
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

        var now = timeProvider.GetUtcNow();

        var submitResult = request.Submit(DateOnly.FromDateTime(now.UtcDateTime), now.UtcDateTime);
        if (submitResult.IsFailure)
        {
            return submitResult;
        }

        return await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
