using BigSolutions.VacaFlow.Domain.Primitives;

namespace BigSolutions.VacaFlow.Domain.Requests.Errors;

/// <summary>
/// Codes and messages from the FRD §7 catalogue and the Backlog.md §3.5
/// microcopy catalogue, reproduced verbatim. Only the errors this story
/// exercises — later stories add their own codes as they add behavior
/// (US-015 plan D5).
/// </summary>
public static class RequestErrors
{
    public static readonly Error EndDateBeforeStartDate = new(
        "VF-REQ-001",
        "The end date cannot be earlier than the start date.",
        Field: "endDate");

    public static readonly Error StartDateInPast = new(
        "VF-REQ-002",
        "The start date cannot be in the past.",
        Field: "startDate");

    /// <remarks>
    /// Not in the Backlog.md §3.5 microcopy catalogue — §3.5 only catalogues
    /// the date and reason messages. Coined following the exact style of the
    /// catalogued ones, same precedent as EmployeeErrors.PasswordTooLong.
    /// Pending addition to Backlog.md §3.5 (US-015 plan D7).
    /// </remarks>
    public static readonly Error AbsenceTypeRequired = new(
        "VF-VAL-001",
        "The absence type is required.",
        Field: "absenceTypeId");

    public static readonly Error StartDateRequired = new(
        "VF-VAL-001",
        "The start date is required.",
        Field: "startDate");

    public static readonly Error EndDateRequired = new(
        "VF-VAL-001",
        "The end date is required.",
        Field: "endDate");

    public static readonly Error ReasonRequired = new(
        "VF-VAL-001",
        "The reason is required (1 to 500 characters).",
        Field: "reason");

    /// <remarks>
    /// Field-less: it names the whole operation, not one form field. RULE-03
    /// (US-016) — only a Draft request can be edited.
    /// </remarks>
    public static readonly Error OnlyDraftEditable = new(
        "VF-REQ-003",
        "Only Draft requests can be edited.");

    /// <remarks>
    /// Field-less, same reasoning as OnlyDraftEditable. RULE-04 (US-016) —
    /// the caller must be the request's own owner, compared only against
    /// ICurrentUser.EmployeeId (FR-AUT-010).
    /// </remarks>
    public static readonly Error NotOwner = new(
        "VF-REQ-004",
        "You can only act on your own requests.");

    /// <remarks>
    /// Field-less, same reasoning as OnlyDraftEditable. Raised when
    /// PUT /requests/{id} names an id with no matching row (US-016).
    /// </remarks>
    public static readonly Error NotFound = new(
        "VF-REQ-006",
        "The request was not found.");

    /// <remarks>
    /// Field-less, message parameterized by the attempted transition
    /// (FR-LFC-001, US-018) — the only member here that is a factory rather
    /// than a static readonly instance, since its message interpolates the
    /// two RequestState values involved.
    /// </remarks>
    public static Error InvalidTransition(RequestState from, RequestState to) => new(
        "VF-REQ-005",
        $"This request cannot move from {from} to {to}.");

    /// <remarks>
    /// Field-less. RULE-05 (US-021) — deciding (approve/reject) requires the
    /// request to be Submitted. Deliberately its own code, not a reuse of
    /// InvalidTransition/VF-REQ-005: the FRD catalogue assigns decisions
    /// their own message, and §4.2 reserves VF-REQ-005 for Submit/Cancel's
    /// out-of-table transitions.
    /// </remarks>
    public static readonly Error OnlySubmittedDecidable = new(
        "VF-DEC-001",
        "Only Submitted requests can be approved or rejected.");

    /// <remarks>
    /// Field-less, same reasoning as OnlySubmittedDecidable. RULE-09
    /// (US-021) — a request can be decided only once.
    /// </remarks>
    public static readonly Error AlreadyDecided = new(
        "VF-DEC-005",
        "This request already has a final decision.");

    /// <remarks>
    /// Not in the Backlog.md §3.5 microcopy catalogue — coined following
    /// the exact style of the catalogued ones, same precedent as
    /// AbsenceTypeRequired. FR-DEC-008 (US-021) — the decision comment is
    /// optional but capped at 500 characters when present.
    /// </remarks>
    public static readonly Error CommentTooLong = new(
        "VF-VAL-001",
        "The comment must be at most 500 characters.",
        Field: "comment");
}
