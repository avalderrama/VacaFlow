using BigSolutions.VacaFlow.Domain.Primitives;
using BigSolutions.VacaFlow.Domain.Requests.Errors;

namespace BigSolutions.VacaFlow.Application.Requests;

/// <summary>
/// Input to the create-draft-request use case. <see cref="Validate"/> is
/// structural validation at the application boundary (CA-APP-007, ADR-011):
/// presence only. Carries no employeeId — the owner comes exclusively from
/// ICurrentUser (FR-AUT-010, AC4); No_Contract_Or_Command_Should_Carry_An_Identity_Field
/// enforces this structurally.
/// </summary>
public sealed record CreateRequestCommand(Guid? AbsenceTypeId, DateOnly? StartDate, DateOnly? EndDate, string? Reason)
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
