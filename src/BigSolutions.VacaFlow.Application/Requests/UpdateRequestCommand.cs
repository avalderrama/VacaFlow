using BigSolutions.VacaFlow.Domain.Primitives;
using BigSolutions.VacaFlow.Domain.Requests.Errors;

namespace BigSolutions.VacaFlow.Application.Requests;

/// <summary>
/// Input to the edit-draft-request use case. <see cref="Validate"/> is
/// structural validation at the application boundary (CA-APP-007, ADR-011),
/// identical in shape to CreateRequestCommand.Validate() so an edit failure
/// carries the same field messages as creation (AC4). RequestId comes from
/// the route, not the payload — it identifies the resource, not the actor,
/// so it is not swept by No_Contract_Or_Command_Should_Carry_An_Identity_Field.
/// Carries no employeeId — the owner comes exclusively from ICurrentUser
/// (FR-AUT-010).
/// </summary>
public sealed record UpdateRequestCommand(
    Guid RequestId,
    Guid? AbsenceTypeId,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string? Reason)
{
    private const int MaxReasonLength = 500;

    public Result Validate()
    {
        if (AbsenceTypeId is null || AbsenceTypeId == Guid.Empty)
        {
            return Result.Failure(RequestErrors.AbsenceTypeRequired);
        }

        if (StartDate is null)
        {
            return Result.Failure(RequestErrors.StartDateRequired);
        }

        if (EndDate is null)
        {
            return Result.Failure(RequestErrors.EndDateRequired);
        }

        if (string.IsNullOrWhiteSpace(Reason) || Reason.Trim().Length > MaxReasonLength)
        {
            return Result.Failure(RequestErrors.ReasonRequired);
        }

        return Result.Success();
    }
}
