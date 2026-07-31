using BigSolutions.VacaFlow.Domain.Employees;
using BigSolutions.VacaFlow.Domain.Primitives;
using BigSolutions.VacaFlow.Domain.Requests.Errors;

namespace BigSolutions.VacaFlow.Domain.Requests.Services;

/// <summary>
/// Whether a manager may decide on a request (US-021, RULE-06/RULE-07).
/// This is the system's first domain service (CA-SRV-001): the rule
/// crosses two aggregates (Request, Employee) with no natural owner in
/// either one. Static and pure — no repository, no clock — so it takes
/// already-loaded entities and is testable with three plain objects.
/// </summary>
public static class ApprovalPolicy
{
    public static Result CanDecide(Request request, Employee owner, Employee actingManager)
    {
        if (actingManager.Role is not EmployeeRole.Manager)
        {
            return Result.Failure(ApprovalErrors.NotAManager);
        }

        if (owner.Id == actingManager.Id)
        {
            return Result.Failure(ApprovalErrors.SelfDecisionForbidden);
        }

        if (owner.ManagerId is null)
        {
            return Result.Failure(ApprovalErrors.NoManagerAssigned);
        }

        if (owner.ManagerId != actingManager.Id)
        {
            return Result.Failure(ApprovalErrors.NotAssignedManager);
        }

        return Result.Success();
    }
}
