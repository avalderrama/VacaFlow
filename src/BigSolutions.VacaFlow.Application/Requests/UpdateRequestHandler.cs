using BigSolutions.VacaFlow.Application.Abstractions;
using BigSolutions.VacaFlow.Domain.AbsenceTypes;
using BigSolutions.VacaFlow.Domain.AbsenceTypes.Errors;
using BigSolutions.VacaFlow.Domain.Primitives;
using BigSolutions.VacaFlow.Domain.Requests;
using BigSolutions.VacaFlow.Domain.Requests.Errors;

namespace BigSolutions.VacaFlow.Application.Requests;

/// <summary>
/// Edits an owner's own draft request (US-016, FR-REQ-005—007). The owner
/// comparison (RULE-04) is the only place identity is checked, and it is
/// checked exclusively against ICurrentUser.EmployeeId — never anything from
/// the command.
/// </summary>
public sealed class UpdateRequestHandler(
    ICurrentUser currentUser,
    IAbsenceTypeRepository absenceTypes,
    IRequestRepository requests,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<Result> Handle(UpdateRequestCommand command, CancellationToken cancellationToken)
    {
        var validation = command.Validate();
        if (validation.IsFailure)
        {
            return validation;
        }

        var periodResult = DateRange.Create(command.StartDate!.Value, command.EndDate!.Value);
        if (periodResult.IsFailure)
        {
            return Result.Failure(periodResult.Error);
        }

        var request = await requests.GetByIdAsync(new RequestId(command.RequestId), cancellationToken);
        if (request is null)
        {
            return Result.Failure(RequestErrors.NotFound);
        }

        if (request.OwnerId != currentUser.EmployeeId)
        {
            return Result.Failure(RequestErrors.NotOwner);
        }

        // RULE-03 short-circuits here too, not only inside UpdateDetails
        // below: a request RULE-03 will reject outright should never pay for
        // the ExistsActiveAsync round trip, and — more importantly — should
        // never have that unrelated check silently override VF-REQ-003 with
        // VF-CAT-001 on the wire.
        if (!request.IsEditable)
        {
            return Result.Failure(RequestErrors.OnlyDraftEditable);
        }

        var absenceTypeId = new AbsenceTypeId(command.AbsenceTypeId!.Value);

        if (!await absenceTypes.ExistsActiveAsync(absenceTypeId, cancellationToken))
        {
            return Result.Failure(AbsenceTypeErrors.NotAvailable);
        }

        var now = timeProvider.GetUtcNow();

        var updateResult = request.UpdateDetails(
            absenceTypeId,
            periodResult.Value,
            command.Reason,
            DateOnly.FromDateTime(now.UtcDateTime),
            now.UtcDateTime);

        if (updateResult.IsFailure)
        {
            return updateResult;
        }

        return await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
