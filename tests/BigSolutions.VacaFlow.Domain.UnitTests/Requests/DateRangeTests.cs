using BigSolutions.VacaFlow.Domain.Requests;

namespace BigSolutions.VacaFlow.Domain.UnitTests.Requests;

public sealed class DateRangeTests
{
    private static readonly DateOnly Start = new(2026, 8, 10);

    [Fact]
    public void Create_Should_Fail_When_The_End_Date_Is_Before_The_Start_Date()
    {
        var result = DateRange.Create(Start, Start.AddDays(-1));

        Assert.True(result.IsFailure);
        Assert.Equal("VF-REQ-001", result.Error.Code);
        Assert.Equal("endDate", result.Error.Field);
    }

    [Fact]
    public void Create_Should_Succeed_When_The_End_Date_Equals_The_Start_Date()
    {
        var result = DateRange.Create(Start, Start);

        Assert.True(result.IsSuccess);
        Assert.Equal(Start, result.Value.Start);
        Assert.Equal(Start, result.Value.End);
    }

    [Fact]
    public void Create_Should_Succeed_When_The_End_Date_Is_After_The_Start_Date()
    {
        var result = DateRange.Create(Start, Start.AddDays(2));

        Assert.True(result.IsSuccess);
        Assert.Equal(Start.AddDays(2), result.Value.End);
    }

    [Fact]
    public void Two_Ranges_With_The_Same_Dates_Should_Be_Equal()
    {
        var first = DateRange.Create(Start, Start.AddDays(2)).Value;
        var second = DateRange.Create(Start, Start.AddDays(2)).Value;

        Assert.Equal(first, second);
        Assert.True(first == second);
    }
}
