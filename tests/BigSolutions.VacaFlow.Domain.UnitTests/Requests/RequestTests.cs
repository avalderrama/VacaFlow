using BigSolutions.VacaFlow.Domain.AbsenceTypes;
using BigSolutions.VacaFlow.Domain.Employees;
using BigSolutions.VacaFlow.Domain.Requests;

namespace BigSolutions.VacaFlow.Domain.UnitTests.Requests;

/// <summary>
/// Rule-to-test map required by NFR-MNT-007 ("map tests to rules; the
/// mapping is complete"), covering RULE-01–RULE-09 of Intent.md §7.4 and
/// the T1–T5 transition matrix of FRD.md §4.2.
/// </summary>
/// <remarks>
/// <para>
/// RULE-01 (end ≥ start) is unbreakable by construction — a Request cannot
/// hold an invalid range because DateRange refuses to build one. Covered in
/// <see cref="DateRangeTests"/>, including both boundaries.
/// </para>
/// <para>
/// RULE-02 (no past start) is re-evaluated on every path that accepts a
/// date — Create, UpdateDetails and Submit (OQ-04) — each with the
/// start-equals-today boundary and the day-before failure. Cancel and
/// Decide are writes too but deliberately do not re-check it.
/// </para>
/// <para>
/// RULE-03 (only Draft editable) — <c>UpdateDetails_Should_Fail_When_The_Request_Is_Already_Submitted</c>
/// and <c>UpdateDetails_Should_Fail_When_The_Request_Is_No_Longer_A_Draft</c>, together covering all four non-Draft origins.
/// RULE-05 (only Submitted decidable) — <c>Decide_Should_Fail_When_The_Request_Is_Not_Submitted_And_Has_No_Decision</c>.
/// RULE-09 (one final decision; final states immutable) — <c>Decide_Should_Fail_When_The_Request_Already_Has_A_Final_Decision</c>
/// for the "one decision" half, and for immutability every method that can
/// be aimed at a decided request: <c>Cancel_Should_Fail_When_The_Request_Has_Already_Been_Decided</c>,
/// <c>Submit_Should_Fail_When_The_Request_Has_Already_Been_Decided</c> and
/// the Approved/Rejected rows of <c>UpdateDetails_Should_Fail_When_The_Request_Is_No_Longer_A_Draft</c>.
/// </para>
/// <para>
/// RULE-08 (approving or rejecting creates exactly one Approval record,
/// with the authenticated manager as responsible) is split across two
/// layers. The domain half is here: both <c>Decide_Should_Succeed_*</c>
/// tests assert the record carries the manager it was given, and
/// <c>Decide_Should_Fail_When_The_Request_Already_Has_A_Final_Decision</c>
/// asserts a second decision cannot replace it. That the manager is the
/// *authenticated* one (FR-DEC-006, AC-12) cannot be asserted here — this
/// aggregate is handed an EmployeeId and has no notion of who is signed
/// in — so it is covered by Application.UnitTests
/// (<c>DecideRequestHandlerTests.Handle_Should_Record_The_Authenticated_Manager_As_Responsible</c>),
/// same split as RULE-04 below.
/// </para>
/// <para>
/// RULE-06 and RULE-07 are authorization, decided by ApprovalPolicy rather
/// than by this aggregate — see <see cref="ApprovalPolicyTests"/> for the
/// policy's own branches. Note those tests are not the whole guarantee:
/// <c>CanDecide</c> compares the acting manager against the *owner it is
/// handed*, and never reads the request, so it cannot detect a caller that
/// resolves the wrong owner. Pairing the request with its real owner is the
/// handler's job, and that half is covered by Application.UnitTests
/// (<c>DecideRequestHandlerTests.Handle_Should_Fail_When_The_Owner_Is_Assigned_To_A_Different_Manager</c>
/// and <c>Handle_Should_Fail_When_The_Manager_Acts_On_Their_Own_Request</c>).
/// RULE-04
/// (only the owner acts) is deliberately not a domain rule at all: it needs
/// the caller's identity, which the domain never sees, so it lives in the
/// handlers and is covered by Application.UnitTests
/// (<c>Handle_Should_Fail_When_The_Caller_Is_Not_The_Owner</c> and siblings).
/// </para>
/// <para>
/// Transitions: the five valid ones (T1–T5) each have a passing test, and
/// every invalid origin state is rejected — Submit from Submitted,
/// Cancelled, Approved and Rejected; Cancel from Cancelled, Approved and
/// Rejected; Decide from Draft and Cancelled via its state guard, and from
/// Approved and Rejected via the already-decided guard that fires first.
/// That is the complete invalid set for the three domain methods behind
/// §4.2's four triggers — Approve and Reject both land on <c>Decide</c>.
/// </para>
/// </remarks>
public sealed class RequestTests
{
    private static readonly RequestId Id = new(Guid.NewGuid());
    private static readonly EmployeeId OwnerId = new(Guid.NewGuid());
    private static readonly AbsenceTypeId TypeId = new(Guid.NewGuid());
    private static readonly DateOnly Today = new(2026, 8, 10);
    private static readonly DateTime NowUtc = new(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc);

    private static DateRange PeriodStartingToday() => DateRange.Create(Today, Today.AddDays(2)).Value;

    [Fact]
    public void Create_Should_Succeed_With_Valid_Data()
    {
        var result = Request.Create(Id, OwnerId, TypeId, PeriodStartingToday(), "Family trip", Today, NowUtc);

        Assert.True(result.IsSuccess);
        var request = result.Value;
        Assert.Equal(RequestState.Draft, request.State);
        Assert.Equal(OwnerId, request.OwnerId);
        Assert.Equal(TypeId, request.AbsenceTypeId);
        Assert.Equal("Family trip", request.Reason);
        Assert.Equal(NowUtc, request.CreatedAtUtc);
        Assert.Equal(NowUtc, request.UpdatedAtUtc);
        Assert.Null(request.SubmittedAtUtc);
        Assert.Null(request.ClosedAtUtc);
    }

    [Fact]
    public void Create_Should_Succeed_When_The_Start_Date_Equals_Today()
    {
        var period = DateRange.Create(Today, Today).Value;

        var result = Request.Create(Id, OwnerId, TypeId, period, "Family trip", Today, NowUtc);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Create_Should_Fail_When_The_Start_Date_Is_Before_Today()
    {
        var yesterday = Today.AddDays(-1);
        var period = DateRange.Create(yesterday, Today).Value;

        var result = Request.Create(Id, OwnerId, TypeId, period, "Family trip", Today, NowUtc);

        Assert.True(result.IsFailure);
        Assert.Equal("VF-REQ-002", result.Error.Code);
        Assert.Equal("startDate", result.Error.Field);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Should_Fail_When_The_Reason_Is_Missing(string? reason)
    {
        var result = Request.Create(Id, OwnerId, TypeId, PeriodStartingToday(), reason, Today, NowUtc);

        Assert.True(result.IsFailure);
        Assert.Equal("VF-VAL-001", result.Error.Code);
        Assert.Equal("reason", result.Error.Field);
    }

    [Fact]
    public void Create_Should_Fail_When_The_Reason_Exceeds_500_Characters()
    {
        var tooLong = new string('a', 501);

        var result = Request.Create(Id, OwnerId, TypeId, PeriodStartingToday(), tooLong, Today, NowUtc);

        Assert.True(result.IsFailure);
        Assert.Equal("VF-VAL-001", result.Error.Code);
    }

    [Fact]
    public void Create_Should_Succeed_When_The_Reason_Is_Exactly_500_Characters()
    {
        var maxLength = new string('a', 500);

        var result = Request.Create(Id, OwnerId, TypeId, PeriodStartingToday(), maxLength, Today, NowUtc);

        Assert.True(result.IsSuccess);
        Assert.Equal(maxLength, result.Value.Reason);
    }

    /// <remarks>
    /// The length guard measures the *trimmed* reason, so padding that a
    /// user cannot see must not push an otherwise-valid reason over the
    /// limit — and the stored value is the trimmed one, not what was typed.
    /// Neither half is observable from a reason that has no whitespace to
    /// strip, which is why every other reason test here misses it.
    /// </remarks>
    [Fact]
    public void Create_Should_Trim_The_Reason_Before_Measuring_And_Storing_It()
    {
        var paddedToOverTheLimit = new string('a', 500).PadLeft(505).PadRight(510);

        var result = Request.Create(Id, OwnerId, TypeId, PeriodStartingToday(), paddedToOverTheLimit, Today, NowUtc);

        Assert.True(result.IsSuccess);
        Assert.Equal(new string('a', 500), result.Value.Reason);
    }

    private static readonly DateTime LaterNowUtc = NowUtc.AddHours(3);
    private static readonly AbsenceTypeId OtherTypeId = new(Guid.NewGuid());

    private static Request NewDraft() =>
        Request.Create(Id, OwnerId, TypeId, PeriodStartingToday(), "Family trip", Today, NowUtc).Value;

    [Fact]
    public void UpdateDetails_Should_Succeed_On_A_Draft_With_Valid_Data()
    {
        var request = NewDraft();
        var newPeriod = DateRange.Create(Today.AddDays(1), Today.AddDays(3)).Value;

        var result = request.UpdateDetails(OtherTypeId, newPeriod, "Updated reason", Today, LaterNowUtc);

        Assert.True(result.IsSuccess);
        Assert.Equal(OtherTypeId, request.AbsenceTypeId);
        Assert.Equal(newPeriod, request.Period);
        Assert.Equal("Updated reason", request.Reason);
        Assert.Equal(LaterNowUtc, request.UpdatedAtUtc);
        Assert.Equal(NowUtc, request.CreatedAtUtc);
        Assert.Equal(RequestState.Draft, request.State);
    }

    [Fact]
    public void UpdateDetails_Should_Succeed_When_The_Start_Date_Equals_Today()
    {
        var request = NewDraft();
        var period = DateRange.Create(Today, Today).Value;

        var result = request.UpdateDetails(TypeId, period, "Updated reason", Today, LaterNowUtc);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void UpdateDetails_Should_Fail_When_The_Start_Date_Is_Before_Today()
    {
        var request = NewDraft();
        var yesterday = Today.AddDays(-1);
        var period = DateRange.Create(yesterday, Today).Value;

        var result = request.UpdateDetails(TypeId, period, "Updated reason", Today, LaterNowUtc);

        Assert.True(result.IsFailure);
        Assert.Equal("VF-REQ-002", result.Error.Code);
        Assert.Equal("startDate", result.Error.Field);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateDetails_Should_Fail_When_The_Reason_Is_Missing(string? reason)
    {
        var request = NewDraft();

        var result = request.UpdateDetails(TypeId, PeriodStartingToday(), reason, Today, LaterNowUtc);

        Assert.True(result.IsFailure);
        Assert.Equal("VF-VAL-001", result.Error.Code);
        Assert.Equal("reason", result.Error.Field);
    }

    [Fact]
    public void UpdateDetails_Should_Fail_When_The_Reason_Exceeds_500_Characters()
    {
        var request = NewDraft();
        var tooLong = new string('a', 501);

        var result = request.UpdateDetails(TypeId, PeriodStartingToday(), tooLong, Today, LaterNowUtc);

        Assert.True(result.IsFailure);
        Assert.Equal("VF-VAL-001", result.Error.Code);
    }

    [Fact]
    public void UpdateDetails_Should_Succeed_When_The_Reason_Is_Exactly_500_Characters()
    {
        var request = NewDraft();
        var maxLength = new string('a', 500);

        var result = request.UpdateDetails(TypeId, PeriodStartingToday(), maxLength, Today, LaterNowUtc);

        Assert.True(result.IsSuccess);
        Assert.Equal(maxLength, request.Reason);
    }

    [Fact]
    public void Submit_Should_Succeed_On_A_Draft_With_A_Future_Start_Date()
    {
        var futurePeriod = DateRange.Create(Today.AddDays(5), Today.AddDays(7)).Value;
        var request = Request.Create(Id, OwnerId, TypeId, futurePeriod, "Family trip", Today, NowUtc).Value;

        var result = request.Submit(Today, LaterNowUtc);

        Assert.True(result.IsSuccess);
        Assert.Equal(RequestState.Submitted, request.State);
        Assert.Equal(LaterNowUtc, request.SubmittedAtUtc);
        Assert.Equal(LaterNowUtc, request.UpdatedAtUtc);
        Assert.Equal(NowUtc, request.CreatedAtUtc);
    }

    [Fact]
    public void Submit_Should_Succeed_When_The_Start_Date_Equals_Today()
    {
        var request = NewDraft();

        var result = request.Submit(Today, LaterNowUtc);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Submit_Should_Fail_When_The_Start_Date_Has_Since_Passed()
    {
        var request = NewDraft();
        var tomorrow = Today.AddDays(1);

        var result = request.Submit(tomorrow, LaterNowUtc);

        Assert.True(result.IsFailure);
        Assert.Equal("VF-REQ-002", result.Error.Code);
        Assert.Equal("startDate", result.Error.Field);
        Assert.Equal(RequestState.Draft, request.State);
        Assert.Null(request.SubmittedAtUtc);
    }

    [Fact]
    public void Submit_Should_Fail_When_The_Request_Is_Already_Submitted()
    {
        var request = NewDraft();
        request.Submit(Today, LaterNowUtc);

        var submittedAt = request.SubmittedAtUtc;
        var updatedAt = request.UpdatedAtUtc;

        var result = request.Submit(Today, LaterNowUtc.AddHours(1));

        Assert.True(result.IsFailure);
        Assert.Equal("VF-REQ-005", result.Error.Code);
        Assert.Equal("This request cannot move from Submitted to Submitted.", result.Error.Message);
        Assert.Equal(submittedAt, request.SubmittedAtUtc);
        Assert.Equal(updatedAt, request.UpdatedAtUtc);
    }

    /// <remarks>
    /// Settles US-016 plan D7: RULE-03's "any other state" branch was
    /// unreachable through the aggregate's own public API until Submit
    /// existed. Now it is reached the legitimate way — Create, then Submit,
    /// then attempt UpdateDetails — instead of only via the direct-SQL state
    /// forcing RequestRepositoryTests uses at the integration level (AC5).
    /// </remarks>
    [Fact]
    public void UpdateDetails_Should_Fail_When_The_Request_Is_Already_Submitted()
    {
        var request = NewDraft();
        request.Submit(Today, LaterNowUtc);
        var newPeriod = DateRange.Create(Today.AddDays(1), Today.AddDays(3)).Value;
        var updatedAt = request.UpdatedAtUtc;

        var result = request.UpdateDetails(OtherTypeId, newPeriod, "Updated reason", Today, LaterNowUtc.AddHours(1));

        Assert.True(result.IsFailure);
        Assert.Equal("VF-REQ-003", result.Error.Code);
        Assert.Equal(TypeId, request.AbsenceTypeId);
        Assert.Equal(RequestState.Submitted, request.State);
        Assert.Equal(updatedAt, request.UpdatedAtUtc);
    }

    /// <remarks>
    /// The remaining non-Draft origins for RULE-03 (US-027). IsEditable is a
    /// single "State is Draft" check, so these share a branch with the
    /// Submitted case above — listed separately for the same reason the
    /// Submit theory below is: FRD.md §4.1 marks four states non-editable,
    /// and a reader checking that table against this suite should find all
    /// four named.
    /// </remarks>
    [Theory]
    [InlineData(RequestState.Cancelled)]
    [InlineData(RequestState.Approved)]
    [InlineData(RequestState.Rejected)]
    public void UpdateDetails_Should_Fail_When_The_Request_Is_No_Longer_A_Draft(RequestState origin)
    {
        var request = NewDraft();
        if (origin is RequestState.Cancelled)
        {
            request.Cancel(LaterNowUtc);
        }
        else
        {
            request.Submit(Today, LaterNowUtc);
            var decision = origin is RequestState.Approved ? DecisionType.Approved : DecisionType.Rejected;
            request.Decide(new ApprovalId(Guid.NewGuid()), new EmployeeId(Guid.NewGuid()), decision, null, LaterNowUtc);
        }

        var originalPeriod = request.Period;
        var updatedAt = request.UpdatedAtUtc;
        var rejectedPeriod = DateRange.Create(Today.AddDays(1), Today.AddDays(3)).Value;

        var result = request.UpdateDetails(OtherTypeId, rejectedPeriod, "Updated reason", Today, LaterNowUtc.AddHours(1));

        Assert.True(result.IsFailure);
        Assert.Equal("VF-REQ-003", result.Error.Code);
        Assert.Equal(origin, request.State);
        Assert.Equal(TypeId, request.AbsenceTypeId);
        Assert.Equal(originalPeriod, request.Period);
        Assert.Equal("Family trip", request.Reason);
        Assert.Equal(updatedAt, request.UpdatedAtUtc);
    }

    [Fact]
    public void Cancel_Should_Succeed_On_A_Draft()
    {
        var request = NewDraft();

        var result = request.Cancel(LaterNowUtc);

        Assert.True(result.IsSuccess);
        Assert.Equal(RequestState.Cancelled, request.State);
        Assert.Equal(LaterNowUtc, request.ClosedAtUtc);
        Assert.Equal(LaterNowUtc, request.UpdatedAtUtc);
        Assert.Null(request.SubmittedAtUtc);
        Assert.Equal(NowUtc, request.CreatedAtUtc);
    }

    [Fact]
    public void Cancel_Should_Succeed_On_A_Submitted_Request_And_Preserve_SubmittedAtUtc()
    {
        var request = NewDraft();
        request.Submit(Today, LaterNowUtc);
        var submittedAt = request.SubmittedAtUtc;
        var evenLaterNowUtc = LaterNowUtc.AddHours(1);

        var result = request.Cancel(evenLaterNowUtc);

        Assert.True(result.IsSuccess);
        Assert.Equal(RequestState.Cancelled, request.State);
        Assert.Equal(evenLaterNowUtc, request.ClosedAtUtc);
        Assert.Equal(evenLaterNowUtc, request.UpdatedAtUtc);
        Assert.Equal(submittedAt, request.SubmittedAtUtc);
    }

    /// <remarks>
    /// Pins the deliberate asymmetry the class comment claims (US-027):
    /// Cancel takes no `today` and never re-checks RULE-02, because
    /// withdrawing a request whose start date has already passed is the
    /// ordinary case, not an error (Request.Cancel's own remarks).
    /// </remarks>
    [Fact]
    public void Cancel_Should_Succeed_When_The_Start_Date_Has_Already_Passed()
    {
        var request = NewDraft();
        var afterTheStartDate = LaterNowUtc.AddDays(30);

        var result = request.Cancel(afterTheStartDate);

        Assert.True(result.IsSuccess);
        Assert.Equal(RequestState.Cancelled, request.State);
        Assert.Equal(afterTheStartDate, request.ClosedAtUtc);
    }

    [Fact]
    public void Cancel_Should_Fail_When_The_Request_Is_Already_Cancelled()
    {
        var request = NewDraft();
        request.Cancel(LaterNowUtc);
        var closedAt = request.ClosedAtUtc;
        var updatedAt = request.UpdatedAtUtc;

        var result = request.Cancel(LaterNowUtc.AddHours(1));

        Assert.True(result.IsFailure);
        Assert.Equal("VF-REQ-005", result.Error.Code);
        Assert.Equal("This request cannot move from Cancelled to Cancelled.", result.Error.Message);
        Assert.Equal(closedAt, request.ClosedAtUtc);
        Assert.Equal(updatedAt, request.UpdatedAtUtc);
    }

    [Fact]
    public void Submit_Should_Fail_When_The_Request_Is_Already_Cancelled()
    {
        var request = NewDraft();
        request.Cancel(LaterNowUtc);

        var result = request.Submit(Today, LaterNowUtc.AddHours(1));

        Assert.True(result.IsFailure);
        Assert.Equal("VF-REQ-005", result.Error.Code);
        Assert.Equal("This request cannot move from Cancelled to Submitted.", result.Error.Message);
    }

    /// <remarks>
    /// The last two cells of the invalid-transition matrix (US-027). Submit
    /// guards on a single "not Draft" condition, so these share their branch
    /// with the Submitted and Cancelled cases above — but a reader checking
    /// FRD.md §4.2 against this suite would otherwise find no test naming
    /// Approved or Rejected as a rejected origin. Shaped as a Theory to
    /// match Cancel_Should_Fail_When_The_Request_Has_Already_Been_Decided,
    /// which covers the same two states for the sibling method.
    /// </remarks>
    [Theory]
    [InlineData(DecisionType.Approved, RequestState.Approved)]
    [InlineData(DecisionType.Rejected, RequestState.Rejected)]
    public void Submit_Should_Fail_When_The_Request_Has_Already_Been_Decided(DecisionType decision, RequestState expectedState)
    {
        var request = NewSubmittedRequest();
        request.Decide(new ApprovalId(Guid.NewGuid()), new EmployeeId(Guid.NewGuid()), decision, null, LaterNowUtc);
        var submittedAt = request.SubmittedAtUtc;
        var updatedAt = request.UpdatedAtUtc;

        var result = request.Submit(Today, LaterNowUtc.AddHours(1));

        Assert.True(result.IsFailure);
        Assert.Equal("VF-REQ-005", result.Error.Code);
        Assert.Equal($"This request cannot move from {expectedState} to Submitted.", result.Error.Message);
        Assert.Equal(expectedState, request.State);
        Assert.Equal(submittedAt, request.SubmittedAtUtc);
        Assert.Equal(updatedAt, request.UpdatedAtUtc);
    }

    private Request NewSubmittedRequest()
    {
        var request = NewDraft();
        request.Submit(Today, NowUtc);
        return request;
    }

    [Fact]
    public void Decide_Should_Succeed_When_Approving_A_Submitted_Request()
    {
        var request = NewSubmittedRequest();
        var managerId = new EmployeeId(Guid.NewGuid());
        var approvalId = new ApprovalId(Guid.NewGuid());

        var result = request.Decide(approvalId, managerId, DecisionType.Approved, "Enjoy", LaterNowUtc);

        Assert.True(result.IsSuccess);
        Assert.Equal(RequestState.Approved, request.State);
        Assert.NotNull(request.Approval);
        Assert.Equal(approvalId, request.Approval.Id);
        Assert.Equal(managerId, request.Approval.ResponsibleManagerId);
        Assert.Equal(DecisionType.Approved, request.Approval.Decision);
        Assert.Equal("Enjoy", request.Approval.Comment);
        Assert.Equal(LaterNowUtc, request.Approval.DecidedAtUtc);
        Assert.Equal(LaterNowUtc, request.ClosedAtUtc);
        Assert.Equal(LaterNowUtc, request.UpdatedAtUtc);
        Assert.Equal(NowUtc, request.SubmittedAtUtc);
    }

    [Fact]
    public void Decide_Should_Succeed_When_Rejecting_A_Submitted_Request()
    {
        var request = NewSubmittedRequest();
        var managerId = new EmployeeId(Guid.NewGuid());
        var approvalId = new ApprovalId(Guid.NewGuid());

        var result = request.Decide(approvalId, managerId, DecisionType.Rejected, null, LaterNowUtc);

        Assert.True(result.IsSuccess);
        Assert.Equal(RequestState.Rejected, request.State);
        Assert.NotNull(request.Approval);
        Assert.Equal(managerId, request.Approval.ResponsibleManagerId);
        Assert.Equal(DecisionType.Rejected, request.Approval.Decision);
        Assert.Null(request.Approval.Comment);
    }

    /// <summary>
    /// Only the states reachable with Approval still null (Draft,
    /// Cancelled) exercise this guard through Decide's own sequential
    /// calls — Approved/Rejected always carry a non-null Approval by the
    /// time they exist, so a second Decide on those hits the
    /// already-decided guard first (VF-DEC-005, see the theory below), not
    /// this one. Calling Decide on a Draft to try to reach Approved/
    /// Rejected would itself fail with VF-DEC-001 and mutate nothing —
    /// that used to be exactly the vacuous setup this test had.
    /// </summary>
    [Theory]
    [InlineData(RequestState.Draft)]
    [InlineData(RequestState.Cancelled)]
    public void Decide_Should_Fail_When_The_Request_Is_Not_Submitted_And_Has_No_Decision(RequestState nonSubmittedState)
    {
        var request = NewDraft();
        if (nonSubmittedState is RequestState.Cancelled)
        {
            request.Cancel(NowUtc);
        }

        // Pins the discarded setup: without this the Cancelled row is a
        // duplicate of the Draft row, and stays green even if Cancel stops
        // working, because Decide rejects Draft with the same code.
        Assert.Equal(nonSubmittedState, request.State);

        var result = request.Decide(new ApprovalId(Guid.NewGuid()), new EmployeeId(Guid.NewGuid()), DecisionType.Approved, null, LaterNowUtc);

        Assert.True(result.IsFailure);
        Assert.Equal("VF-DEC-001", result.Error.Code);
        Assert.Equal("Only Submitted requests can be approved or rejected.", result.Error.Message);
        Assert.Null(request.Approval);
    }

    [Theory]
    [InlineData(DecisionType.Approved, RequestState.Approved)]
    [InlineData(DecisionType.Rejected, RequestState.Rejected)]
    public void Decide_Should_Fail_When_The_Request_Already_Has_A_Final_Decision(DecisionType firstDecision, RequestState expectedState)
    {
        var request = NewSubmittedRequest();
        var managerId = new EmployeeId(Guid.NewGuid());
        request.Decide(new ApprovalId(Guid.NewGuid()), managerId, firstDecision, "First", LaterNowUtc);
        var originalApproval = request.Approval;

        var result = request.Decide(new ApprovalId(Guid.NewGuid()), managerId, DecisionType.Rejected, "Second", LaterNowUtc.AddHours(1));

        Assert.True(result.IsFailure);
        Assert.Equal("VF-DEC-005", result.Error.Code);
        Assert.Equal("This request already has a final decision.", result.Error.Message);
        Assert.Same(originalApproval, request.Approval);
        Assert.Equal(expectedState, request.State);
    }

    /// <remarks>
    /// The Decide half of the same asymmetry <c>Cancel_Should_Succeed_When_The_Start_Date_Has_Already_Passed</c>
    /// pins: a manager deciding a request whose dates have since passed is
    /// ordinary, so Decide takes no `today` and never re-checks RULE-02.
    /// </remarks>
    [Fact]
    public void Decide_Should_Succeed_When_The_Start_Date_Has_Already_Passed()
    {
        var request = NewSubmittedRequest();
        var afterTheStartDate = LaterNowUtc.AddDays(30);

        var result = request.Decide(new ApprovalId(Guid.NewGuid()), new EmployeeId(Guid.NewGuid()), DecisionType.Approved, null, afterTheStartDate);

        Assert.True(result.IsSuccess);
        Assert.Equal(RequestState.Approved, request.State);
        Assert.Equal(afterTheStartDate, request.Approval!.DecidedAtUtc);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Decide_Should_Normalize_A_Blank_Comment_To_Null(string? comment)
    {
        var request = NewSubmittedRequest();

        request.Decide(new ApprovalId(Guid.NewGuid()), new EmployeeId(Guid.NewGuid()), DecisionType.Approved, comment, LaterNowUtc);

        Assert.Null(request.Approval!.Comment);
    }

    [Fact]
    public void Decide_Should_Trim_The_Comment()
    {
        var request = NewSubmittedRequest();

        request.Decide(new ApprovalId(Guid.NewGuid()), new EmployeeId(Guid.NewGuid()), DecisionType.Approved, "  Enjoy  ", LaterNowUtc);

        Assert.Equal("Enjoy", request.Approval!.Comment);
    }

    /// <remarks>
    /// Settles the debt this class's own history carried: Cancel from
    /// Approved/Rejected was unreachable through the aggregate's own public
    /// API until Decide existed to produce those states. Now reached the
    /// legitimate way.
    /// </remarks>
    [Theory]
    [InlineData(DecisionType.Approved, RequestState.Approved)]
    [InlineData(DecisionType.Rejected, RequestState.Rejected)]
    public void Cancel_Should_Fail_When_The_Request_Has_Already_Been_Decided(DecisionType decision, RequestState expectedState)
    {
        var request = NewSubmittedRequest();
        request.Decide(new ApprovalId(Guid.NewGuid()), new EmployeeId(Guid.NewGuid()), decision, null, LaterNowUtc);

        var result = request.Cancel(LaterNowUtc.AddHours(1));

        Assert.True(result.IsFailure);
        Assert.Equal("VF-REQ-005", result.Error.Code);
        Assert.Equal($"This request cannot move from {expectedState} to Cancelled.", result.Error.Message);
    }
}
