using BigSolutions.VacaFlow.Application.Abstractions;
using BigSolutions.VacaFlow.Domain.AbsenceTypes;
using BigSolutions.VacaFlow.Domain.Employees;
using BigSolutions.VacaFlow.Domain.Primitives;
using BigSolutions.VacaFlow.Domain.Requests;

namespace BigSolutions.VacaFlow.Application.Requests;

/// <summary>
/// Lists the requests visible to the authenticated caller (US-020,
/// FR-VIS-001). No input: identity and role come only from ICurrentUser
/// (FR-AUT-010) — the caller cannot influence the result through any
/// parameter. An Employee sees only their own requests, in every state. A
/// Manager sees the union of those same own requests plus the Submitted
/// requests of the employees assigned to them — the request list screen
/// derives "the queue" from this payload by filtering on employee.id
/// (US-020 plan D3).
/// </summary>
public sealed class ListVisibleRequestsHandler(
    ICurrentUser currentUser,
    IRequestRepository requests,
    IEmployeeRepository employees,
    IAbsenceTypeRepository absenceTypes)
{
    public async Task<Result<IReadOnlyList<RequestSummaryDto>>> Handle(CancellationToken cancellationToken)
    {
        var owned = await requests.ListOwnedByAsync(currentUser.EmployeeId, cancellationToken);

        var visible = owned;
        if (currentUser.Role == EmployeeRole.Manager)
        {
            var pending = await requests.ListPendingForManagerAsync(currentUser.EmployeeId, cancellationToken);
            visible = [.. pending, .. owned];
            visible = [.. visible.OrderByDescending(request => request.CreatedAtUtc)];
        }

        var ownerIds = visible.Select(request => request.OwnerId).Distinct().ToList();
        var typeIds = visible.Select(request => request.AbsenceTypeId).Distinct().ToList();

        var employeesById = (await employees.ListByIdsAsync(ownerIds, cancellationToken))
            .ToDictionary(employee => employee.Id);
        var typesById = (await absenceTypes.ListByIdsAsync(typeIds, cancellationToken))
            .ToDictionary(type => type.Id);

        var dtos = visible
            .Select(request => ToSummaryDto(request, employeesById[request.OwnerId], typesById[request.AbsenceTypeId]))
            .ToList();

        return Result.Success<IReadOnlyList<RequestSummaryDto>>(dtos);
    }

    private static RequestSummaryDto ToSummaryDto(Request request, Employee owner, AbsenceType absenceType) => new(
        request.Id.Value,
        new AbsenceTypeSummaryDto(absenceType.Id.Value, absenceType.Code.Value, absenceType.Name),
        request.Period.Start,
        request.Period.End,
        request.Reason,
        request.State.ToString(),
        new EmployeeSummaryDto(owner.Id.Value, owner.FullName),
        request.CreatedAtUtc);
}
