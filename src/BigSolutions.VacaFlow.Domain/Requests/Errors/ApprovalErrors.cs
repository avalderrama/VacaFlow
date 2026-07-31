using BigSolutions.VacaFlow.Domain.Primitives;

namespace BigSolutions.VacaFlow.Domain.Requests.Errors;

/// <summary>
/// Authorization errors for ApprovalPolicy (US-021). Codes and messages
/// from the FRD §7 catalogue, reproduced verbatim.
/// </summary>
public static class ApprovalErrors
{
    /// <remarks>Field-less. RULE-06 (US-021) — only a manager may decide.</remarks>
    public static readonly Error NotAManager = new(
        "VF-DEC-002",
        "Only a manager can approve or reject a request.");

    /// <remarks>
    /// Field-less. RULE-06 (US-021) — the acting manager must be the one
    /// assigned to the request's owner.
    /// </remarks>
    public static readonly Error NotAssignedManager = new(
        "VF-DEC-003",
        "You are not the manager assigned to this employee.");

    /// <remarks>Field-less. RULE-07 (US-021) — a manager cannot decide their own request.</remarks>
    public static readonly Error SelfDecisionForbidden = new(
        "VF-DEC-004",
        "You cannot decide on your own request.");

    /// <remarks>
    /// Field-less. Fail-closed branch for an owner with no assigned manager
    /// (OQ-01, unresolved by the sponsor — "must not be silently
    /// defaulted"). Deliberately reuses NotAssignedManager's code/message:
    /// the catalogue has no dedicated code for this branch, and the
    /// statement is literally true when nobody is assigned (US-021 plan
    /// OQ-B). Kept as a distinct member so the branch's traceability
    /// (SAD.md §5.4) survives even though the wire response is identical.
    /// </remarks>
    public static readonly Error NoManagerAssigned = new(
        "VF-DEC-003",
        "You are not the manager assigned to this employee.");
}
