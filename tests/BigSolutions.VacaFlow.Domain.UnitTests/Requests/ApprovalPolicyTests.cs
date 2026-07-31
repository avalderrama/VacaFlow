using BigSolutions.VacaFlow.Domain.AbsenceTypes;
using BigSolutions.VacaFlow.Domain.Employees;
using BigSolutions.VacaFlow.Domain.Requests;
using BigSolutions.VacaFlow.Domain.Requests.Services;

namespace BigSolutions.VacaFlow.Domain.UnitTests.Requests;

public sealed class ApprovalPolicyTests
{
    private static readonly DateOnly Today = new(2026, 8, 10);
    private static readonly DateTime NowUtc = new(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc);

    private static Employee NewEmployee(EmployeeRole role, EmployeeId? managerId = null)
    {
        var employee = Employee.Create(
            new EmployeeId(Guid.NewGuid()), "Test Employee", Email.Create($"{Guid.NewGuid():N}@vacaflow.test").Value, role).Value;

        if (managerId is not null)
        {
            employee.AssignManager(managerId.Value);
        }

        return employee;
    }

    private static Request NewRequest(EmployeeId owner)
    {
        var period = DateRange.Create(Today, Today.AddDays(2)).Value;
        return Request.Create(
            new RequestId(Guid.NewGuid()), owner, new AbsenceTypeId(Guid.NewGuid()), period, "Family trip", Today, NowUtc).Value;
    }

    [Fact]
    public void CanDecide_Should_Fail_When_The_Acting_Employee_Is_Not_A_Manager()
    {
        var manager = NewEmployee(EmployeeRole.Manager);
        var owner = NewEmployee(EmployeeRole.Employee, manager.Id);
        var actingEmployee = NewEmployee(EmployeeRole.Employee);
        var request = NewRequest(owner.Id);

        var result = ApprovalPolicy.CanDecide(request, owner, actingEmployee);

        Assert.True(result.IsFailure);
        Assert.Equal("VF-DEC-002", result.Error.Code);
    }

    [Fact]
    public void CanDecide_Should_Fail_When_The_Manager_Acts_On_Their_Own_Request()
    {
        var manager = NewEmployee(EmployeeRole.Manager);
        var request = NewRequest(manager.Id);

        var result = ApprovalPolicy.CanDecide(request, manager, manager);

        Assert.True(result.IsFailure);
        Assert.Equal("VF-DEC-004", result.Error.Code);
    }

    [Fact]
    public void CanDecide_Should_Fail_Closed_When_The_Owner_Has_No_Assigned_Manager()
    {
        var owner = NewEmployee(EmployeeRole.Employee);
        var actingManager = NewEmployee(EmployeeRole.Manager);
        var request = NewRequest(owner.Id);

        var result = ApprovalPolicy.CanDecide(request, owner, actingManager);

        Assert.True(result.IsFailure);
        Assert.Equal("VF-DEC-003", result.Error.Code);
    }

    [Fact]
    public void CanDecide_Should_Fail_When_The_Manager_Is_Not_The_One_Assigned_To_The_Owner()
    {
        var assignedManager = NewEmployee(EmployeeRole.Manager);
        var otherManager = NewEmployee(EmployeeRole.Manager);
        var owner = NewEmployee(EmployeeRole.Employee, assignedManager.Id);
        var request = NewRequest(owner.Id);

        var result = ApprovalPolicy.CanDecide(request, owner, otherManager);

        Assert.True(result.IsFailure);
        Assert.Equal("VF-DEC-003", result.Error.Code);
    }

    [Fact]
    public void CanDecide_Should_Succeed_When_The_Manager_Is_Assigned_To_The_Owner()
    {
        var manager = NewEmployee(EmployeeRole.Manager);
        var owner = NewEmployee(EmployeeRole.Employee, manager.Id);
        var request = NewRequest(owner.Id);

        var result = ApprovalPolicy.CanDecide(request, owner, manager);

        Assert.True(result.IsSuccess);
    }

    /// <summary>
    /// Self gains over the not-assigned-manager branch (SAD.md §5.4
    /// order). A plain "manager decides their own request" (with no
    /// manager of their own) cannot distinguish self-decision-forbidden
    /// from not-assigned-manager, since both would fire on the same
    /// employee id comparison once ManagerId is null too — the acting
    /// manager here has their own boss assigned, so this is the only
    /// arrangement that actually exercises branch 2 winning over branch 4.
    /// </summary>
    [Fact]
    public void CanDecide_Should_Prefer_Self_Decision_Forbidden_Over_Not_Assigned_Manager()
    {
        var boss = NewEmployee(EmployeeRole.Manager);
        var manager = NewEmployee(EmployeeRole.Manager, boss.Id);
        var request = NewRequest(manager.Id);

        var result = ApprovalPolicy.CanDecide(request, manager, manager);

        Assert.True(result.IsFailure);
        Assert.Equal("VF-DEC-004", result.Error.Code);
    }
}
