using BigSolutions.VacaFlow.Domain.Primitives;
using BigSolutions.VacaFlow.Domain.Requests;
using BigSolutions.VacaFlow.Domain.Requests.Errors;

namespace BigSolutions.VacaFlow.Application.Requests;

/// <summary>
/// Input to the decide-a-request use case (US-021), shared by approve and
/// reject (SAD.md §6.1) — Decision is wired by the endpoint, never read
/// from the payload. RequestId comes from the route, not the payload — it
/// identifies the resource, not the actor. Carries no employeeId — the
/// responsible manager comes exclusively from ICurrentUser (FR-DEC-006).
/// </summary>
public sealed record DecideRequestCommand(Guid RequestId, DecisionType Decision, string? Comment)
{
    private const int MaxCommentLength = 500;

    public Result Validate()
    {
        if (!string.IsNullOrWhiteSpace(Comment) && Comment.Trim().Length > MaxCommentLength)
        {
            return Result.Failure(RequestErrors.CommentTooLong);
        }

        return Result.Success();
    }
}
