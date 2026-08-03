using BigSolutions.VacaFlow.Application.Requests;
using BigSolutions.VacaFlow.Application.UnitTests.Authentication.Fakes;
using BigSolutions.VacaFlow.Application.UnitTests.Requests.Fakes;
using BigSolutions.VacaFlow.Domain.AbsenceTypes;
using BigSolutions.VacaFlow.Domain.Employees;
using BigSolutions.VacaFlow.Domain.Requests;

namespace BigSolutions.VacaFlow.Application.UnitTests.Requests;

public sealed class DecideRequestHandlerTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 8, 10, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 8, 10);
    private static readonly AbsenceTypeId TypeId = new(Guid.NewGuid());
    private static readonly Guid FixedApprovalId = Guid.NewGuid();

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

    private static Request NewSubmittedRequest(EmployeeId owner)
    {
        var request = Request.Create(
            new RequestId(Guid.NewGuid()), owner, TypeId,
            DateRange.Create(Today, Today.AddDays(2)).Value, "Family trip",
            Today, FixedNow.UtcDateTime).Value;
        request.Submit(Today, FixedNow.UtcDateTime);
        return request;
    }

    private static DecideRequestHandler CreateHandler(
        FakeRequestRepository requests,
        FakeEmployeeRepository employees,
        EmployeeId actingUser) =>
        new(
            new FakeCurrentUser(actingUser, EmployeeRole.Manager),
            requests,
            employees,
            new FakeUnitOfWork(),
            new FakeIdGenerator(FixedApprovalId),
            new FixedTimeProvider(FixedNow));

    [Fact]
    public async Task Handle_Should_Succeed_When_The_Assigned_Manager_Approves_A_Submitted_Request()
    {
        var manager = NewEmployee(EmployeeRole.Manager);
        var owner = NewEmployee(EmployeeRole.Employee, manager.Id);
        var request = NewSubmittedRequest(owner.Id);
        var requests = new FakeRequestRepository(request);
        var employees = new FakeEmployeeRepository().WithEmployee(owner).WithEmployee(manager);
        var handler = CreateHandler(requests, employees, manager.Id);

        var result = await handler.Handle(
            new DecideRequestCommand(request.Id.Value, DecisionType.Approved, "Enjoy"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(RequestState.Approved, request.State);
        Assert.NotNull(request.Approval);
    }

    /// <summary>AC2: the responsible manager is the authenticated user, never a payload value — there is no other source it could come from.</summary>
    [Fact]
    public async Task Handle_Should_Record_The_Authenticated_Manager_As_Responsible()
    {
        var manager = NewEmployee(EmployeeRole.Manager);
        var owner = NewEmployee(EmployeeRole.Employee, manager.Id);
        var request = NewSubmittedRequest(owner.Id);
        var requests = new FakeRequestRepository(request);
        var employees = new FakeEmployeeRepository().WithEmployee(owner).WithEmployee(manager);
        var handler = CreateHandler(requests, employees, manager.Id);

        await handler.Handle(new DecideRequestCommand(request.Id.Value, DecisionType.Approved, null), CancellationToken.None);

        Assert.Equal(manager.Id, request.Approval!.ResponsibleManagerId);
        Assert.Equal(new ApprovalId(FixedApprovalId), request.Approval.Id);
    }

    /// <summary>AC1: rejecting a Submitted request with a comment transitions to Rejected and the Approval record carries the comment.</summary>
    [Fact]
    public async Task Handle_Should_Succeed_When_The_Assigned_Manager_Rejects_A_Submitted_Request_With_A_Comment()
    {
        var manager = NewEmployee(EmployeeRole.Manager);
        var owner = NewEmployee(EmployeeRole.Employee, manager.Id);
        var request = NewSubmittedRequest(owner.Id);
        var requests = new FakeRequestRepository(request);
        var employees = new FakeEmployeeRepository().WithEmployee(owner).WithEmployee(manager);
        var handler = CreateHandler(requests, employees, manager.Id);

        var result = await handler.Handle(
            new DecideRequestCommand(request.Id.Value, DecisionType.Rejected, "No coverage that week"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(RequestState.Rejected, request.State);
        Assert.NotNull(request.Approval);
        Assert.Equal(DecisionType.Rejected, request.Approval!.Decision);
        Assert.Equal("No coverage that week", request.Approval.Comment);
        Assert.Equal(manager.Id, request.Approval.ResponsibleManagerId);
    }

    /// <summary>AC2: the comment is optional for a rejection too — it succeeds with no comment.</summary>
    [Fact]
    public async Task Handle_Should_Succeed_When_The_Assigned_Manager_Rejects_A_Submitted_Request_Without_A_Comment()
    {
        var manager = NewEmployee(EmployeeRole.Manager);
        var owner = NewEmployee(EmployeeRole.Employee, manager.Id);
        var request = NewSubmittedRequest(owner.Id);
        var requests = new FakeRequestRepository(request);
        var employees = new FakeEmployeeRepository().WithEmployee(owner).WithEmployee(manager);
        var handler = CreateHandler(requests, employees, manager.Id);

        var result = await handler.Handle(
            new DecideRequestCommand(request.Id.Value, DecisionType.Rejected, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(RequestState.Rejected, request.State);
        Assert.Null(request.Approval!.Comment);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Request_Does_Not_Exist()
    {
        var manager = NewEmployee(EmployeeRole.Manager);
        var requests = new FakeRequestRepository();
        var employees = new FakeEmployeeRepository().WithEmployee(manager);
        var handler = CreateHandler(requests, employees, manager.Id);

        var result = await handler.Handle(new DecideRequestCommand(Guid.NewGuid(), DecisionType.Approved, null), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VF-REQ-006", result.Error.Code);
    }

    /// <summary>
    /// Defensive branch (S1): not reachable through any real flow (a
    /// Request.OwnerId is FK-constrained to an existing Employee row), but
    /// exercised so the failure mode is verified rather than merely
    /// asserted in a comment — a generic not-found, not an unhandled
    /// exception, if the owner's Employee row were ever missing.
    /// </summary>
    [Fact]
    public async Task Handle_Should_Fail_When_The_Owner_Employee_Row_Is_Missing()
    {
        var manager = NewEmployee(EmployeeRole.Manager);
        var missingOwnerId = new EmployeeId(Guid.NewGuid());
        var request = NewSubmittedRequest(missingOwnerId);
        var requests = new FakeRequestRepository(request);
        var employees = new FakeEmployeeRepository().WithEmployee(manager);
        var handler = CreateHandler(requests, employees, manager.Id);

        var result = await handler.Handle(
            new DecideRequestCommand(request.Id.Value, DecisionType.Approved, null), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VF-REQ-006", result.Error.Code);
    }

    /// <summary>
    /// Defensive branch (S1): not reachable in practice (a valid session
    /// cookie implies a backing Employee row), but exercised for the same
    /// reason as the owner-missing case above.
    /// </summary>
    [Fact]
    public async Task Handle_Should_Fail_When_The_Acting_Managers_Employee_Row_Is_Missing()
    {
        var owner = NewEmployee(EmployeeRole.Employee);
        var request = NewSubmittedRequest(owner.Id);
        var requests = new FakeRequestRepository(request);
        var employees = new FakeEmployeeRepository().WithEmployee(owner);
        var missingActingManagerId = new EmployeeId(Guid.NewGuid());
        var handler = CreateHandler(requests, employees, missingActingManagerId);

        var result = await handler.Handle(
            new DecideRequestCommand(request.Id.Value, DecisionType.Approved, null), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VF-REQ-006", result.Error.Code);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Request_Is_Not_Submitted()
    {
        var manager = NewEmployee(EmployeeRole.Manager);
        var owner = NewEmployee(EmployeeRole.Employee, manager.Id);
        var request = Request.Create(
            new RequestId(Guid.NewGuid()), owner.Id, TypeId,
            DateRange.Create(Today, Today.AddDays(2)).Value, "Family trip", Today, FixedNow.UtcDateTime).Value;
        var requests = new FakeRequestRepository(request);
        var employees = new FakeEmployeeRepository().WithEmployee(owner).WithEmployee(manager);
        var handler = CreateHandler(requests, employees, manager.Id);

        var result = await handler.Handle(
            new DecideRequestCommand(request.Id.Value, DecisionType.Approved, null), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VF-DEC-001", result.Error.Code);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Acting_User_Is_Not_A_Manager()
    {
        var manager = NewEmployee(EmployeeRole.Manager);
        var owner = NewEmployee(EmployeeRole.Employee, manager.Id);
        var actingEmployee = NewEmployee(EmployeeRole.Employee);
        var request = NewSubmittedRequest(owner.Id);
        var requests = new FakeRequestRepository(request);
        var employees = new FakeEmployeeRepository().WithEmployee(owner).WithEmployee(actingEmployee);
        var handler = new DecideRequestHandler(
            new FakeCurrentUser(actingEmployee.Id, EmployeeRole.Employee),
            requests,
            employees,
            new FakeUnitOfWork(),
            new FakeIdGenerator(FixedApprovalId),
            new FixedTimeProvider(FixedNow));

        var result = await handler.Handle(
            new DecideRequestCommand(request.Id.Value, DecisionType.Approved, null), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VF-DEC-002", result.Error.Code);
        Assert.Equal(RequestState.Submitted, request.State);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Manager_Acts_On_Their_Own_Request()
    {
        var manager = NewEmployee(EmployeeRole.Manager);
        var request = NewSubmittedRequest(manager.Id);
        var requests = new FakeRequestRepository(request);
        var employees = new FakeEmployeeRepository().WithEmployee(manager);
        var handler = CreateHandler(requests, employees, manager.Id);

        var result = await handler.Handle(
            new DecideRequestCommand(request.Id.Value, DecisionType.Approved, null), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VF-DEC-004", result.Error.Code);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Owner_Is_Assigned_To_A_Different_Manager()
    {
        var assignedManager = NewEmployee(EmployeeRole.Manager);
        var otherManager = NewEmployee(EmployeeRole.Manager);
        var owner = NewEmployee(EmployeeRole.Employee, assignedManager.Id);
        var request = NewSubmittedRequest(owner.Id);
        var requests = new FakeRequestRepository(request);
        var employees = new FakeEmployeeRepository().WithEmployee(owner).WithEmployee(otherManager);
        var handler = CreateHandler(requests, employees, otherManager.Id);

        var result = await handler.Handle(
            new DecideRequestCommand(request.Id.Value, DecisionType.Approved, null), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VF-DEC-003", result.Error.Code);
    }

    [Fact]
    public async Task Handle_Should_Fail_Closed_When_The_Owner_Has_No_Assigned_Manager()
    {
        var owner = NewEmployee(EmployeeRole.Employee);
        var actingManager = NewEmployee(EmployeeRole.Manager);
        var request = NewSubmittedRequest(owner.Id);
        var requests = new FakeRequestRepository(request);
        var employees = new FakeEmployeeRepository().WithEmployee(owner).WithEmployee(actingManager);
        var handler = CreateHandler(requests, employees, actingManager.Id);

        var result = await handler.Handle(
            new DecideRequestCommand(request.Id.Value, DecisionType.Approved, null), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VF-DEC-003", result.Error.Code);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Request_Is_Already_Decided()
    {
        var manager = NewEmployee(EmployeeRole.Manager);
        var owner = NewEmployee(EmployeeRole.Employee, manager.Id);
        var request = NewSubmittedRequest(owner.Id);
        request.Decide(new ApprovalId(Guid.NewGuid()), manager.Id, DecisionType.Approved, null, FixedNow.UtcDateTime);
        var requests = new FakeRequestRepository(request);
        var employees = new FakeEmployeeRepository().WithEmployee(owner).WithEmployee(manager);
        var handler = CreateHandler(requests, employees, manager.Id);

        var result = await handler.Handle(
            new DecideRequestCommand(request.Id.Value, DecisionType.Rejected, null), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VF-DEC-005", result.Error.Code);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Comment_Exceeds_500_Characters()
    {
        var manager = NewEmployee(EmployeeRole.Manager);
        var owner = NewEmployee(EmployeeRole.Employee, manager.Id);
        var request = NewSubmittedRequest(owner.Id);
        var requests = new FakeRequestRepository(request);
        var employees = new FakeEmployeeRepository().WithEmployee(owner).WithEmployee(manager);
        var handler = CreateHandler(requests, employees, manager.Id);
        var tooLong = new string('a', 501);

        var result = await handler.Handle(
            new DecideRequestCommand(request.Id.Value, DecisionType.Approved, tooLong), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VF-VAL-001", result.Error.Code);
        Assert.Equal("comment", result.Error.Field);
        Assert.Equal(RequestState.Submitted, request.State);
    }

    [Fact]
    public async Task Handle_Should_Succeed_When_The_Comment_Is_Exactly_500_Characters()
    {
        var manager = NewEmployee(EmployeeRole.Manager);
        var owner = NewEmployee(EmployeeRole.Employee, manager.Id);
        var request = NewSubmittedRequest(owner.Id);
        var requests = new FakeRequestRepository(request);
        var employees = new FakeEmployeeRepository().WithEmployee(owner).WithEmployee(manager);
        var handler = CreateHandler(requests, employees, manager.Id);
        var maxLength = new string('a', 500);

        var result = await handler.Handle(
            new DecideRequestCommand(request.Id.Value, DecisionType.Approved, maxLength), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }
}
