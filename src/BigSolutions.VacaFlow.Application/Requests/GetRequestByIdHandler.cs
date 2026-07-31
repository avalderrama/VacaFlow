using BigSolutions.VacaFlow.Application.Abstractions;
using BigSolutions.VacaFlow.Domain.Primitives;
using BigSolutions.VacaFlow.Domain.Requests;
using BigSolutions.VacaFlow.Domain.Requests.Errors;

namespace BigSolutions.VacaFlow.Application.Requests;

/// <summary>
/// Reads a single request, restricted to its own owner (US-017, RULE-04).
/// No input beyond the route id, so — like GetCurrentUserHandler — there is
/// no command record (ADR-011 governs commands, not single-id queries).
/// Reuses IRequestRepository.GetByIdAsync, added by US-016; the port does
/// not grow for this story.
/// </summary>
public sealed class GetRequestByIdHandler(ICurrentUser currentUser, IRequestRepository requests)
{
    public async Task<Result<RequestDetailDto>> Handle(Guid requestId, CancellationToken cancellationToken)
    {
        var request = await requests.GetByIdAsync(new RequestId(requestId), cancellationToken);
        if (request is null)
        {
            return Result.Failure<RequestDetailDto>(RequestErrors.NotFound);
        }

        if (request.OwnerId != currentUser.EmployeeId)
        {
            return Result.Failure<RequestDetailDto>(RequestErrors.NotOwner);
        }

        return Result.Success(new RequestDetailDto(
            request.Id.Value,
            request.AbsenceTypeId.Value,
            request.Period.Start,
            request.Period.End,
            request.Reason,
            request.State.ToString()));
    }
}
