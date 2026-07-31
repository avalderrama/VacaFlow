using BigSolutions.VacaFlow.Domain.Employees;
using BigSolutions.VacaFlow.Domain.Primitives;

namespace BigSolutions.VacaFlow.Domain.Requests;

/// <summary>
/// A manager's decision on a request (US-021). Child entity of
/// <see cref="Request"/>, not a separate aggregate root (ADR-004) — it is
/// created and persisted only as part of Request.Decide, one transactional
/// fact together with the state change (FR-DEC-009).
/// </summary>
public sealed class Approval : Entity<ApprovalId>
{
    private Approval(
        ApprovalId id,
        EmployeeId responsibleManagerId,
        DecisionType decision,
        string? comment,
        DateTime decidedAtUtc)
        : base(id)
    {
        ResponsibleManagerId = responsibleManagerId;
        Decision = decision;
        Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        DecidedAtUtc = decidedAtUtc;
    }

    /// <summary>Required by EF Core. Never call it from application code (CA-DOM-002).</summary>
    private Approval()
    {
    }

    public EmployeeId ResponsibleManagerId { get; private set; }

    public DecisionType Decision { get; private set; }

    public string? Comment { get; private set; }

    public DateTime DecidedAtUtc { get; private set; }

    /// <summary>
    /// Internal: the only legitimate caller is Request.Decide — this
    /// keeps the aggregate boundary compiler-enforced (ADR-004). Comment
    /// length (≤500) is validated in Application (FR-DEC-008); this only
    /// normalizes (trim, blank → null).
    /// </summary>
    internal static Approval Create(
        ApprovalId id,
        EmployeeId responsibleManagerId,
        DecisionType decision,
        string? comment,
        DateTime decidedAtUtc) =>
        new(id, responsibleManagerId, decision, comment, decidedAtUtc);
}
