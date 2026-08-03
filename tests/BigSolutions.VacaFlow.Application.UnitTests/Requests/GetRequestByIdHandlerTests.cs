using BigSolutions.VacaFlow.Application.Requests;
using BigSolutions.VacaFlow.Application.UnitTests.Authentication.Fakes;
using BigSolutions.VacaFlow.Application.UnitTests.Requests.Fakes;
using BigSolutions.VacaFlow.Domain.AbsenceTypes;
using BigSolutions.VacaFlow.Domain.Employees;
using BigSolutions.VacaFlow.Domain.Requests;

namespace BigSolutions.VacaFlow.Application.UnitTests.Requests;

public sealed class GetRequestByIdHandlerTests
{
    private static readonly EmployeeId OwnerId = new(Guid.NewGuid());
    private static readonly AbsenceTypeId TypeId = new(Guid.NewGuid());
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

    private Request NewDraft() => Request.Create(
        new RequestId(Guid.NewGuid()), OwnerId, TypeId,
        DateRange.Create(Today, Today.AddDays(2)).Value, "Family trip",
        Today, NowUtc).Value;

    private Request NewSubmitted()
    {
        var request = NewDraft();
        request.Submit(Today, NowUtc);
        return request;
    }

    [Fact]
    public async Task Handle_Should_Return_The_Full_Detail_For_The_Owner()
    {
        var draft = NewDraft();
        var requests = new FakeRequestRepository(draft);
        var employees = new FakeEmployeeRepository();
        var handler = new GetRequestByIdHandler(new FakeCurrentUser(OwnerId), requests, employees);

        var result = await handler.Handle(draft.Id.Value, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var dto = result.Value;
        Assert.Equal(draft.Id.Value, dto.Id);
        Assert.Equal(TypeId.Value, dto.AbsenceTypeId);
        Assert.Equal(Today, dto.StartDate);
        Assert.Equal(Today.AddDays(2), dto.EndDate);
        Assert.Equal("Family trip", dto.Reason);
        Assert.Equal("Draft", dto.State);
        Assert.Null(dto.Approval);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Request_Does_Not_Exist()
    {
        var requests = new FakeRequestRepository();
        var employees = new FakeEmployeeRepository();
        var handler = new GetRequestByIdHandler(new FakeCurrentUser(OwnerId), requests, employees);

        var result = await handler.Handle(Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VF-REQ-006", result.Error.Code);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Caller_Is_Not_The_Owner()
    {
        var draft = NewDraft();
        var requests = new FakeRequestRepository(draft);
        var employees = new FakeEmployeeRepository();
        var handler = new GetRequestByIdHandler(new FakeCurrentUser(new EmployeeId(Guid.NewGuid())), requests, employees);

        var result = await handler.Handle(draft.Id.Value, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VF-REQ-004", result.Error.Code);
    }

    /// <summary>The owner guard runs before the manager lookup even on a decided request — a non-owner never triggers the Employee lookup or sees the approval block.</summary>
    [Fact]
    public async Task Handle_Should_Fail_When_The_Caller_Is_Not_The_Owner_Of_A_Decided_Request()
    {
        var manager = NewEmployee(EmployeeRole.Manager);
        var request = NewSubmitted();
        request.Decide(new ApprovalId(Guid.NewGuid()), manager.Id, DecisionType.Approved, "Enjoy", NowUtc);
        var requests = new FakeRequestRepository(request);
        var employees = new FakeEmployeeRepository().WithEmployee(manager);
        var handler = new GetRequestByIdHandler(new FakeCurrentUser(new EmployeeId(Guid.NewGuid())), requests, employees);

        var result = await handler.Handle(request.Id.Value, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VF-REQ-004", result.Error.Code);
    }

    /// <summary>AC2-AC4: an Approved request carries the decision block with the manager's name and comment.</summary>
    [Fact]
    public async Task Handle_Should_Include_The_Approval_Block_When_The_Request_Was_Approved_With_A_Comment()
    {
        var manager = NewEmployee(EmployeeRole.Manager);
        var request = NewSubmitted();
        request.Decide(new ApprovalId(Guid.NewGuid()), manager.Id, DecisionType.Approved, "Enjoy", NowUtc);
        var requests = new FakeRequestRepository(request);
        var employees = new FakeEmployeeRepository().WithEmployee(manager);
        var handler = new GetRequestByIdHandler(new FakeCurrentUser(OwnerId), requests, employees);

        var result = await handler.Handle(request.Id.Value, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var approval = result.Value.Approval;
        Assert.NotNull(approval);
        Assert.Equal(manager.FullName, approval.ResponsibleManagerName);
        Assert.Equal("Approved", approval.Decision);
        Assert.Equal("Enjoy", approval.Comment);
        Assert.Equal(NowUtc, approval.DecidedAtUtc);
    }

    /// <summary>AC4: no comment yields a null Comment, not an empty string.</summary>
    [Fact]
    public async Task Handle_Should_Include_The_Approval_Block_Without_A_Comment_When_Rejected_Without_One()
    {
        var manager = NewEmployee(EmployeeRole.Manager);
        var request = NewSubmitted();
        request.Decide(new ApprovalId(Guid.NewGuid()), manager.Id, DecisionType.Rejected, null, NowUtc);
        var requests = new FakeRequestRepository(request);
        var employees = new FakeEmployeeRepository().WithEmployee(manager);
        var handler = new GetRequestByIdHandler(new FakeCurrentUser(OwnerId), requests, employees);

        var result = await handler.Handle(request.Id.Value, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var approval = result.Value.Approval;
        Assert.NotNull(approval);
        Assert.Equal("Rejected", approval.Decision);
        Assert.Null(approval.Comment);
    }

    /// <summary>AC6: a Submitted request has no decision block.</summary>
    [Fact]
    public async Task Handle_Should_Not_Include_An_Approval_Block_For_A_Submitted_Request()
    {
        var request = NewSubmitted();
        var requests = new FakeRequestRepository(request);
        var employees = new FakeEmployeeRepository();
        var handler = new GetRequestByIdHandler(new FakeCurrentUser(OwnerId), requests, employees);

        var result = await handler.Handle(request.Id.Value, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.Approval);
    }
}
