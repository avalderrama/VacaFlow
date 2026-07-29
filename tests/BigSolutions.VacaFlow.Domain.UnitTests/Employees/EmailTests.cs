using BigSolutions.VacaFlow.Domain.Employees;

namespace BigSolutions.VacaFlow.Domain.UnitTests.Employees;

public sealed class EmailTests
{
    [Theory]
    [InlineData("name@company.com")]
    [InlineData("first.last@sub.company.co")]
    public void Create_Should_Succeed_For_A_Valid_Address(string value)
    {
        var result = Email.Create(value);

        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-email")]
    [InlineData("missing-domain@")]
    [InlineData("@missing-local.com")]
    [InlineData("has spaces@company.com")]
    public void Create_Should_Fail_For_An_Invalid_Address(string? value)
    {
        var result = Email.Create(value);

        Assert.True(result.IsFailure);
        Assert.Equal("VF-VAL-001", result.Error.Code);
        Assert.Equal("email", result.Error.Field);
    }

    [Fact]
    public void Create_Should_Fail_When_The_Address_Exceeds_The_Maximum_Length()
    {
        var tooLong = new string('a', 190) + "@company.com"; // > 200 characters

        var result = Email.Create(tooLong);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Create_Should_Normalize_The_Address_To_Lower_Case()
    {
        var result = Email.Create("Bob@Company.COM");

        Assert.True(result.IsSuccess);
        Assert.Equal("bob@company.com", result.Value.Value);
    }

    [Fact]
    public void Two_Emails_Differing_Only_By_Case_Should_Be_Equal()
    {
        var first = Email.Create("Bob@Company.com").Value;
        var second = Email.Create("bob@company.com").Value;

        Assert.Equal(first, second);
    }
}
