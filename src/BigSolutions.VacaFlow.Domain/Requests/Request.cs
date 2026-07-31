using BigSolutions.VacaFlow.Domain.AbsenceTypes;
using BigSolutions.VacaFlow.Domain.Employees;
using BigSolutions.VacaFlow.Domain.Primitives;
using BigSolutions.VacaFlow.Domain.Requests.Errors;

namespace BigSolutions.VacaFlow.Domain.Requests;

/// <summary>
/// An employee's absence request (Intent.md §7.1, FRD.md §4). Aggregate root:
/// the request's own lifecycle changes independently of the employee or the
/// absence type it references (SAD.md §5.1). This story only exercises
/// <see cref="Create"/> — Submit/Cancel/Decide arrive with their own stories
/// (US-016/US-018/US-019, US-015 plan D5).
/// </summary>
public sealed class Request : AggregateRoot<RequestId>
{
    private const int MaxReasonLength = 500;

    private Request(
        RequestId id,
        EmployeeId ownerId,
        AbsenceTypeId absenceTypeId,
        DateRange period,
        string reason,
        DateTime nowUtc)
        : base(id)
    {
        OwnerId = ownerId;
        AbsenceTypeId = absenceTypeId;
        Period = period;
        Reason = reason;
        State = RequestState.Draft;
        CreatedAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
    }

    /// <summary>Required by EF Core. Never call it from application code (CA-DOM-002).</summary>
    private Request()
    {
        Period = null!;
        Reason = string.Empty;
    }

    public EmployeeId OwnerId { get; private set; }

    public AbsenceTypeId AbsenceTypeId { get; private set; }

    public DateRange Period { get; private set; }

    public string Reason { get; private set; }

    public RequestState State { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public DateTime? SubmittedAtUtc { get; private set; }

    public DateTime? ClosedAtUtc { get; private set; }

    public static Result<Request> Create(
        RequestId id,
        EmployeeId ownerId,
        AbsenceTypeId absenceTypeId,
        DateRange period,
        string? reason,
        DateOnly today,
        DateTime nowUtc)
    {
        if (period.Start < today)
        {
            return Result.Failure<Request>(RequestErrors.StartDateInPast);
        }

        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > MaxReasonLength)
        {
            return Result.Failure<Request>(RequestErrors.ReasonRequired);
        }

        return Result.Success(new Request(id, ownerId, absenceTypeId, period, reason.Trim(), nowUtc));
    }
}
