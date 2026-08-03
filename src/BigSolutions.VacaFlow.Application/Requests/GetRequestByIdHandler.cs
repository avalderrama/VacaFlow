using BigSolutions.VacaFlow.Application.Abstractions;
using BigSolutions.VacaFlow.Domain.Employees;
using BigSolutions.VacaFlow.Domain.Primitives;
using BigSolutions.VacaFlow.Domain.Requests;
using BigSolutions.VacaFlow.Domain.Requests.Errors;

namespace BigSolutions.VacaFlow.Application.Requests;

/// <summary>
/// Reads a single request, restricted to its own owner (US-017, RULE-04).
/// No input beyond the route id, so — like GetCurrentUserHandler — there is
/// no command record (ADR-011 governs commands, not single-id queries).
/// Reuses IRequestRepository.GetByIdAsync, added by US-016, and
/// IEmployeeRepository.GetByIdAsync, added by US-021, to resolve the
/// responsible manager's name for the decision block (US-025); neither
/// port grows for this story. The manager's Employee row is assumed
/// present whenever an Approval exists — Approvals.ResponsibleManagerId
/// has a Restrict foreign key, so its absence is impossible by
/// construction, not a defensive branch to write.
/// </summary>
public sealed class GetRequestByIdHandler(
    ICurrentUser currentUser,
    IRequestRepository requests,
    IEmployeeRepository employees)
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

        RequestApprovalDto? approvalDto = null;
        if (request.Approval is { } approval)
        {
            var manager = await employees.GetByIdAsync(approval.ResponsibleManagerId, cancellationToken);
            approvalDto = new RequestApprovalDto(
                manager!.FullName,
                approval.Decision.ToString(),
                approval.Comment,
                approval.DecidedAtUtc);
        }

        return Result.Success(new RequestDetailDto(
            request.Id.Value,
            request.AbsenceTypeId.Value,
            request.Period.Start,
            request.Period.End,
            request.Reason,
            request.State.ToString(),
            approvalDto));
    }
}
