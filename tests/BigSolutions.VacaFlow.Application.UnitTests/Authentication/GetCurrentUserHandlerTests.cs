using BigSolutions.VacaFlow.Application.Authentication;
using BigSolutions.VacaFlow.Application.UnitTests.Authentication.Fakes;
using BigSolutions.VacaFlow.Domain.Employees;

namespace BigSolutions.VacaFlow.Application.UnitTests.Authentication;

public sealed class GetCurrentUserHandlerTests
{
    private static readonly EmployeeId KnownId = new(Guid.Parse("00000000-0000-0000-0000-000000000042"));

    private static Employee BuildEmployee(EmployeeId id, string fullName = "Laura Méndez") =>
        Employee.Create(
            id,
            fullName,
            Email.Create("laura.mendez@vacaflow.test").Value,
            EmployeeRole.Manager).Value;

    [Fact]
    public async Task Handle_Should_Return_The_Persisted_Employee_Data_For_The_Current_User()
    {
        var employee = BuildEmployee(KnownId);
        var repository = new FakeEmployeeRepository().WithEmployee(employee);
        var handler = new GetCurrentUserHandler(new FakeCurrentUser(KnownId, EmployeeRole.Manager), repository);

        var result = await handler.Handle(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(KnownId.Value, result.Value.Id);
        Assert.Equal("Laura Méndez", result.Value.FullName);
        Assert.Equal("laura.mendez@vacaflow.test", result.Value.Email);
        Assert.Equal("Manager", result.Value.Role);
    }

    /// <summary>
    /// The claims on the cookie carry only the id and the role (TE-011, D1) —
    /// full name and email must come from the repository, never be echoed
    /// back from the identity the caller presented. A FakeCurrentUser that
    /// disagreed with the seeded employee would make this test fail if the
    /// handler ever took a shortcut through the claims instead.
    /// </summary>
    [Fact]
    public async Task Handle_Should_Return_The_Aggregate_Data_Not_Anything_From_The_Current_User_Port()
    {
        var employee = BuildEmployee(KnownId, "Real Name From The Database");
        var repository = new FakeEmployeeRepository().WithEmployee(employee);
        var handler = new GetCurrentUserHandler(new FakeCurrentUser(KnownId, EmployeeRole.Employee), repository);

        var result = await handler.Handle(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Real Name From The Database", result.Value.FullName);
        // Role reported is the employee's own persisted role (Manager), not
        // ICurrentUser.Role (Employee) — the DTO answers from the aggregate.
        Assert.Equal("Manager", result.Value.Role);
    }

    [Fact]
    public async Task Handle_Should_Fail_With_NotAuthenticated_When_The_Employee_No_Longer_Exists()
    {
        var repository = new FakeEmployeeRepository();
        var handler = new GetCurrentUserHandler(new FakeCurrentUser(KnownId), repository);

        var result = await handler.Handle(CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VF-AUT-004", result.Error.Code);
    }
}
