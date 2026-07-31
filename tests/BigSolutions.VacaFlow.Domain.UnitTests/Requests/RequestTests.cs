using BigSolutions.VacaFlow.Domain.AbsenceTypes;
using BigSolutions.VacaFlow.Domain.Employees;
using BigSolutions.VacaFlow.Domain.Requests;

namespace BigSolutions.VacaFlow.Domain.UnitTests.Requests;

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

    private static readonly DateTime LaterNowUtc = NowUtc.AddHours(3);
    private static readonly AbsenceTypeId OtherTypeId = new(Guid.NewGuid());

    private static Request NewDraft() =>
        Request.Create(Id, OwnerId, TypeId, PeriodStartingToday(), "Family trip", Today, NowUtc).Value;

    /// <remarks>
    /// RULE-03's "any other state" branch (UpdateDetails on a non-Draft
    /// request → VF-REQ-003) is not exercised in this class: Draft is the
    /// only state reachable through the aggregate's own public API today —
    /// Submit arrives with US-018 (US-016 plan D7). It IS covered
    /// end-to-end by Infrastructure.IntegrationTests's
    /// RequestRepositoryTests.UpdateDetails_On_A_Row_Forced_To_Submitted_Should_Fail_With_VF_REQ_003,
    /// which forces a Submitted row directly in the SqliteDatabaseFixture
    /// database (not the Api.FunctionalTests WebApplicationFactory one,
    /// where out-of-band SQL proved unreliable). When US-018 delivers
    /// Submit, this class gains a sibling that chains Submit + UpdateDetails
    /// to cover the guard from a pure domain unit test too.
    /// </remarks>
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
}
