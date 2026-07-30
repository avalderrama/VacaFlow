using BigSolutions.VacaFlow.Domain.AbsenceTypes;

namespace BigSolutions.VacaFlow.Domain.UnitTests.AbsenceTypes;

public sealed class AbsenceTypeTests
{
    [Fact]
    public void Create_Should_Succeed_With_Valid_Data()
    {
        var result = AbsenceType.Create(new AbsenceTypeId(Guid.NewGuid()), AbsenceTypeCode.Vacation, "Vacation");

        Assert.True(result.IsSuccess);
        Assert.Equal(AbsenceTypeCode.Vacation, result.Value.Code);
        Assert.Equal("Vacation", result.Value.Name);
        Assert.True(result.Value.IsActive);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Should_Fail_When_The_Name_Is_Missing(string? name)
    {
        var result = AbsenceType.Create(new AbsenceTypeId(Guid.NewGuid()), AbsenceTypeCode.Vacation, name);

        Assert.True(result.IsFailure);
        Assert.Equal("VF-INT-003", result.Error.Code);
    }

    [Fact]
    public void Create_Should_Fail_When_The_Name_Exceeds_120_Characters()
    {
        var tooLong = new string('a', 121);

        var result = AbsenceType.Create(new AbsenceTypeId(Guid.NewGuid()), AbsenceTypeCode.Vacation, tooLong);

        Assert.True(result.IsFailure);
    }
}
