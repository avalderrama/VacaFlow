using BigSolutions.VacaFlow.Application.Abstractions;
using BigSolutions.VacaFlow.Domain.AbsenceTypes;
using BigSolutions.VacaFlow.Domain.AbsenceTypes.Errors;
using BigSolutions.VacaFlow.Domain.Primitives;
using BigSolutions.VacaFlow.Domain.Requests;

namespace BigSolutions.VacaFlow.Application.Requests;

/// <summary>
/// Creates a request in Draft state, owned by the caller (FR-REQ-001,
/// US-015). The owner comes exclusively from ICurrentUser.EmployeeId, never
/// from the command (FR-AUT-010).
/// </summary>
public sealed class CreateRequestHandler(
    ICurrentUser currentUser,
    IAbsenceTypeRepository absenceTypes,
    IRequestRepository requests,
    IUnitOfWork unitOfWork,
    IIdGenerator idGenerator,
    TimeProvider timeProvider)
{
    public async Task<Result<Guid>> Handle(CreateRequestCommand command, CancellationToken cancellationToken)
    {
        var validation = command.Validate();
        if (validation.IsFailure)
        {
            return Result.Failure<Guid>(validation.Error);
        }

        var periodResult = DateRange.Create(command.StartDate!.Value, command.EndDate!.Value);
        if (periodResult.IsFailure)
        {
            return Result.Failure<Guid>(periodResult.Error);
        }

        var absenceTypeId = new AbsenceTypeId(command.AbsenceTypeId!.Value);

        if (!await absenceTypes.ExistsActiveAsync(absenceTypeId, cancellationToken))
        {
            return Result.Failure<Guid>(AbsenceTypeErrors.NotAvailable);
        }

        var now = timeProvider.GetUtcNow();

        var requestResult = Request.Create(
            new RequestId(idGenerator.NewId()),
            currentUser.EmployeeId,
            absenceTypeId,
            periodResult.Value,
            command.Reason,
            DateOnly.FromDateTime(now.UtcDateTime),
            now.UtcDateTime);

        if (requestResult.IsFailure)
        {
            return Result.Failure<Guid>(requestResult.Error);
        }

        var request = requestResult.Value;
        requests.Add(request);

        var saveResult = await unitOfWork.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
        {
            return Result.Failure<Guid>(saveResult.Error);
        }

        return Result.Success(request.Id.Value);
    }
}
