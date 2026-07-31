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
}
