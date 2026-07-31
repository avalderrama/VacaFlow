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

        var result = request.UpdateDetails(OtherTypeId, newPeriod, "Updated reason", Today, LaterNowUtc.AddHours(1));

        Assert.True(result.IsFailure);
        Assert.Equal("VF-REQ-003", result.Error.Code);
        Assert.Equal(TypeId, request.AbsenceTypeId);
        Assert.Equal(RequestState.Submitted, request.State);
    }
}
