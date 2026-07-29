using BigSolutions.VacaFlow.Domain.Employees;

namespace BigSolutions.VacaFlow.Domain.UnitTests.Employees;

public sealed class EmployeeTests
{
    private static readonly Email SampleEmail = Email.Create("employee@vacaflow.test").Value;

    [Fact]
    public void Create_Should_Succeed_With_Valid_Data()
    {
        var result = Employee.Create(
            new EmployeeId(Guid.NewGuid()), "Carlos Ruiz", SampleEmail, EmployeeRole.Employee);

        Assert.True(result.IsSuccess);
        Assert.Equal("Carlos Ruiz", result.Value.FullName);
        Assert.Equal(SampleEmail, result.Value.Email);
        Assert.Equal(EmployeeRole.Employee, result.Value.Role);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Should_Fail_When_The_Full_Name_Is_Missing(string? fullName)
    {
        var result = Employee.Create(new EmployeeId(Guid.NewGuid()), fullName, SampleEmail, EmployeeRole.Employee);

        Assert.True(result.IsFailure);
        Assert.Equal("VF-VAL-001", result.Error.Code);
        Assert.Equal("fullName", result.Error.Field);
    }

    [Fact]
    public void Create_Should_Fail_When_The_Full_Name_Exceeds_120_Characters()
    {
        var tooLong = new string('a', 121);

        var result = Employee.Create(new EmployeeId(Guid.NewGuid()), tooLong, SampleEmail, EmployeeRole.Employee);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void A_Newly_Created_Employee_Should_Be_Active_With_No_Manager()
    {
        var employee = Employee.Create(
            new EmployeeId(Guid.NewGuid()), "Ana Torres", SampleEmail, EmployeeRole.Employee).Value;

        Assert.True(employee.IsActive);
        Assert.Null(employee.ManagerId);
    }
}
