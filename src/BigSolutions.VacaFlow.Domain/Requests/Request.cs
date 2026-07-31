using BigSolutions.VacaFlow.Domain.AbsenceTypes;
using BigSolutions.VacaFlow.Domain.Employees;
using BigSolutions.VacaFlow.Domain.Primitives;
using BigSolutions.VacaFlow.Domain.Requests.Errors;

namespace BigSolutions.VacaFlow.Domain.Requests;

/// <summary>
/// An employee's absence request (Intent.md §7.1, FRD.md §4). Aggregate root:
/// the request's own lifecycle changes independently of the employee or the
/// absence type it references (SAD.md §5.1). Exercises <see cref="Create"/>
/// <see cref="UpdateDetails"/>, <see cref="Submit"/>, <see cref="Cancel"/>
/// and <see cref="Decide"/> (US-015 plan D5).
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

    /// <summary>RULE-03 (US-016): only a Draft request can be edited.</summary>
    public bool IsEditable => State is RequestState.Draft;

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public DateTime? SubmittedAtUtc { get; private set; }

    public DateTime? ClosedAtUtc { get; private set; }

    public Approval? Approval { get; private set; }

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

    /// <summary>
    /// Edits an owner's own draft (US-016, FR-REQ-005—007). RULE-03 is
    /// checked first — the state governs before the content does. RULE-01 is
    /// already unbreakable by construction of <paramref name="period"/>;
    /// RULE-02 and the reason backstop are re-evaluated with the exact
    /// checks <see cref="Create"/> uses, so edit failures carry the same
    /// field messages as creation (AC4). Ownership (RULE-04) is an
    /// Application concern — the handler compares against
    /// ICurrentUser.EmployeeId before calling this.
    /// </summary>
    public Result UpdateDetails(
        AbsenceTypeId absenceTypeId,
        DateRange period,
        string? reason,
        DateOnly today,
        DateTime nowUtc)
    {
        if (!IsEditable)
        {
            return Result.Failure(RequestErrors.OnlyDraftEditable);
        }

        if (period.Start < today)
        {
            return Result.Failure(RequestErrors.StartDateInPast);
        }

        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > MaxReasonLength)
        {
            return Result.Failure(RequestErrors.ReasonRequired);
        }

        AbsenceTypeId = absenceTypeId;
        Period = period;
        Reason = reason.Trim();
        UpdatedAtUtc = nowUtc;

        return Result.Success();
    }

    /// <summary>
    /// Submits an owner's own draft for approval (US-018, transition T1,
    /// FR-LFC-001—004). The state governs before the date does — an
    /// already-decided or already-submitted request fails on the transition
    /// itself, never on a re-check of content. RULE-02 is re-evaluated with
    /// the same comparison <see cref="Create"/>/<see cref="UpdateDetails"/>
    /// use (OQ-04), so a stale draft submitted after its start date has
    /// passed carries the same VF-REQ-002. Ownership (RULE-04) is an
    /// Application concern, checked by the handler before this is called.
    /// </summary>
    public Result Submit(DateOnly today, DateTime nowUtc)
    {
        if (State is not RequestState.Draft)
        {
            return Result.Failure(RequestErrors.InvalidTransition(State, RequestState.Submitted));
        }

        if (Period.Start < today)
        {
            return Result.Failure(RequestErrors.StartDateInPast);
        }

        State = RequestState.Submitted;
        SubmittedAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;

        return Result.Success();
    }

    /// <summary>
    /// Cancels an owner's own request (US-019, transitions T2/T5,
    /// FR-LFC-005). Draft and Submitted are the only cancellable states —
    /// exactly the complement of "final" (Approved/Rejected/Cancelled,
    /// FRD.md §4.1) — so any other state fails on the transition itself,
    /// with no re-check of dates or content (cancelling something whose
    /// start date has already passed is the ordinary case, not an error).
    /// SubmittedAtUtc is left untouched if already set — it is a historical
    /// fact, not a status flag. Ownership (RULE-04) is an Application
    /// concern, checked by the handler before this is called.
    /// </summary>
    public Result Cancel(DateTime nowUtc)
    {
        if (State is not (RequestState.Draft or RequestState.Submitted))
        {
            return Result.Failure(RequestErrors.InvalidTransition(State, RequestState.Cancelled));
        }

        State = RequestState.Cancelled;
        ClosedAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;

        return Result.Success();
    }

    /// <summary>
    /// A manager decides on this request (US-021, transitions T3/T4,
    /// RULE-05/RULE-09). State and the resulting Approval record are one
    /// transactional fact (FR-DEC-009): both statements below belong to a
    /// single method, so no caller can observe one without the other.
    /// Authorization (RULE-06/07 — is the caller a manager, is this their
    /// own request, are they assigned to the owner) is Application's
    /// concern via ApprovalPolicy, checked before this is called.
    /// SubmittedAtUtc is left untouched — a historical fact, same
    /// reasoning as Cancel.
    /// </summary>
    public Result Decide(
        ApprovalId approvalId,
        EmployeeId responsibleManagerId,
        DecisionType decision,
        string? comment,
        DateTime nowUtc)
    {
        // Checked before the state guard: once Approval is set, State is no
        // longer Submitted either (they change together), so checking state
        // first would make this branch unreachable through the aggregate's
        // own sequential calls — "already decided" (AC7) is a more specific
        // and more informative answer than "not submitted" whenever a
        // decision already exists.
        if (Approval is not null)
        {
            return Result.Failure(RequestErrors.AlreadyDecided);
        }

        if (State is not RequestState.Submitted)
        {
            return Result.Failure(RequestErrors.OnlySubmittedDecidable);
        }

        Approval = Approval.Create(approvalId, responsibleManagerId, decision, comment, nowUtc);
        State = decision is DecisionType.Approved ? RequestState.Approved : RequestState.Rejected;
        ClosedAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;

        return Result.Success();
    }
}
