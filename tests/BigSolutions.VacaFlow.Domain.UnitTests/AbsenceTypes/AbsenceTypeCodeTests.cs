using BigSolutions.VacaFlow.Domain.AbsenceTypes;

namespace BigSolutions.VacaFlow.Domain.UnitTests.AbsenceTypes;

public sealed class AbsenceTypeCodeTests
{
    [Theory]
    [InlineData("VACATION")]
    [InlineData("PERSONAL_LEAVE")]
    [InlineData("SICK_LEAVE")]
    public void Create_Should_Succeed_For_A_Known_Code(string value)
    {
        var result = AbsenceTypeCode.Create(value);

        Assert.True(result.IsSuccess);
        Assert.Equal(value, result.Value.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("vacation")]
    [InlineData("HOLIDAY")]
    public void Create_Should_Fail_For_Anything_Not_A_Known_Code(string? value)
    {
        var result = AbsenceTypeCode.Create(value);

        Assert.True(result.IsFailure);
        Assert.Equal("VF-INT-001", result.Error.Code);
    }

    [Fact]
    public void Two_Codes_With_The_Same_Value_Should_Be_Equal()
    {
        var first = AbsenceTypeCode.Create("VACATION").Value;
        var second = AbsenceTypeCode.Create("VACATION").Value;

        Assert.Equal(first, second);
        Assert.Equal(AbsenceTypeCode.Vacation, first);
    }
}
