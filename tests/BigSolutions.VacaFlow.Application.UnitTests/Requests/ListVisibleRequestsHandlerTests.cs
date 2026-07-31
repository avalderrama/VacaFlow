using BigSolutions.VacaFlow.Application.Requests;
using BigSolutions.VacaFlow.Application.UnitTests.AbsenceTypes.Fakes;
using BigSolutions.VacaFlow.Application.UnitTests.Authentication.Fakes;
using BigSolutions.VacaFlow.Application.UnitTests.Requests.Fakes;
using BigSolutions.VacaFlow.Domain.AbsenceTypes;
using BigSolutions.VacaFlow.Domain.Employees;
using BigSolutions.VacaFlow.Domain.Requests;

namespace BigSolutions.VacaFlow.Application.UnitTests.Requests;

public sealed class ListVisibleRequestsHandlerTests
{
    private static readonly DateTime NowUtc = new(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Today = new(2026, 8, 10);

    private readonly EmployeeId _managerId = new(Guid.NewGuid());
    private readonly EmployeeId _employeeId = new(Guid.NewGuid());
    private readonly EmployeeId _otherEmployeeId = new(Guid.NewGuid());

    private readonly AbsenceType _type =
        AbsenceType.Create(new AbsenceTypeId(Guid.NewGuid()), AbsenceTypeCode.Vacation, "Vacation").Value;

    private Employee NewEmployee(EmployeeId id, string fullName) =>
        Employee.Create(id, fullName, Email.Create($"{id.Value:N}@vacaflow.test").Value, EmployeeRole.Employee).Value;

    private Request NewRequest(EmployeeId owner, DateTime createdAt, AbsenceTypeId? typeId = null) => Request.Create(
        new RequestId(Guid.NewGuid()), owner, typeId ?? _type.Id,
        DateRange.Create(Today, Today.AddDays(2)).Value, "Family trip",
        Today, createdAt).Value;

    private ListVisibleRequestsHandler CreateHandler(
        FakeRequestRepository requests,
        FakeEmployeeRepository employees,
        EmployeeId actingUser,
        EmployeeRole role) =>
        new(
            new FakeCurrentUser(actingUser, role),
            requests,
            employees,
            new FakeAbsenceTypeRepository(_type));

    [Fact]
    public async Task Handle_As_Employee_Returns_Only_Their_Own_Requests_In_Every_State()
    {
        var employee = NewEmployee(_employeeId, "Carlos Ruiz");
        var own = NewRequest(_employeeId, NowUtc);
        own.Submit(Today, NowUtc);
        var ownDraft = NewRequest(_employeeId, NowUtc.AddMinutes(-5));
        var other = NewRequest(_otherEmployeeId, NowUtc);
        var requests = new FakeRequestRepository(own, ownDraft, other);
        var employees = new FakeEmployeeRepository().WithEmployee(employee);
        var handler = CreateHandler(requests, employees, _employeeId, EmployeeRole.Employee);

        var result = await handler.Handle(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.All(result.Value, dto => Assert.Equal(_employeeId.Value, dto.Employee.Id));
        Assert.Equal(own.Id.Value, result.Value[0].Id);
    }

    [Fact]
    public async Task Handle_As_Manager_Returns_Submitted_Requests_Of_Assigned_Employees()
    {
        var manager = NewEmployee(_managerId, "Laura Méndez");
        var employee = NewEmployee(_employeeId, "Carlos Ruiz");
        var submitted = NewRequest(_employeeId, NowUtc);
        submitted.Submit(Today, NowUtc);
        var requests = new FakeRequestRepository(submitted).WithManagerAssignment(_employeeId, _managerId);
        var employees = new FakeEmployeeRepository().WithEmployee(manager).WithEmployee(employee);
        var handler = CreateHandler(requests, employees, _managerId, EmployeeRole.Manager);

        var result = await handler.Handle(CancellationToken.None);

        Assert.True(result.IsSuccess);
        var dto = Assert.Single(result.Value);
        Assert.Equal(submitted.Id.Value, dto.Id);
        Assert.Equal("Carlos Ruiz", dto.Employee.FullName);
        Assert.Equal(_type.Code.Value, dto.AbsenceType.Code);
        Assert.Equal(_type.Name, dto.AbsenceType.Name);
    }

    [Fact]
    public async Task Handle_As_Manager_Excludes_Draft_And_Cancelled_And_Unassigned_Employees()
    {
        var manager = NewEmployee(_managerId, "Laura Méndez");
        var assigned = NewEmployee(_employeeId, "Carlos Ruiz");
        var unassigned = NewEmployee(_otherEmployeeId, "Unassigned Employee");

        var draft = NewRequest(_employeeId, NowUtc);
        var cancelled = NewRequest(_employeeId, NowUtc.AddMinutes(-1));
        cancelled.Submit(Today, NowUtc.AddMinutes(-1));
        cancelled.Cancel(NowUtc);
        var unassignedSubmitted = NewRequest(_otherEmployeeId, NowUtc);
        unassignedSubmitted.Submit(Today, NowUtc);

        var requests = new FakeRequestRepository(draft, cancelled, unassignedSubmitted)
            .WithManagerAssignment(_employeeId, _managerId);
        var employees = new FakeEmployeeRepository().WithEmployee(manager).WithEmployee(assigned).WithEmployee(unassigned);
        var handler = CreateHandler(requests, employees, _managerId, EmployeeRole.Manager);

        var result = await handler.Handle(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    /// <summary>
    /// D3's union is only proven when both branches actually contribute:
    /// the manager's own request AND the assigned employee's queue item
    /// must both appear, in one interleaved CreatedAtUtc-descending order
    /// (S1) — not just each branch tested in isolation with the other one
    /// empty, which would pass even if the union collapsed to either side.
    /// </summary>
    [Fact]
    public async Task Handle_As_Manager_Returns_The_Union_Of_Their_Own_Requests_And_The_Team_Queue_In_One_Order()
    {
        var manager = NewEmployee(_managerId, "Laura Méndez");
        var assigned = NewEmployee(_employeeId, "Carlos Ruiz");

        var ownDraft = NewRequest(_managerId, NowUtc.AddMinutes(-1));
        var queueLatest = NewRequest(_employeeId, NowUtc);
        queueLatest.Submit(Today, NowUtc);
        var queueOldest = NewRequest(_employeeId, NowUtc.AddMinutes(-2));
        queueOldest.Submit(Today, NowUtc.AddMinutes(-2));

        var requests = new FakeRequestRepository(ownDraft, queueLatest, queueOldest)
            .WithManagerAssignment(_employeeId, _managerId);
        var employees = new FakeEmployeeRepository().WithEmployee(manager).WithEmployee(assigned);
        var handler = CreateHandler(requests, employees, _managerId, EmployeeRole.Manager);

        var result = await handler.Handle(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.Count);
        Assert.Equal(3, result.Value.Select(dto => dto.Id).Distinct().Count());
        Assert.Equal(
            [queueLatest.Id.Value, ownDraft.Id.Value, queueOldest.Id.Value],
            result.Value.Select(dto => dto.Id));
        Assert.Equal(_managerId.Value, Assert.Single(result.Value, dto => dto.Id == ownDraft.Id.Value).Employee.Id);
    }

    [Fact]
    public async Task Handle_As_Employee_Never_Calls_The_Manager_Queue_Query()
    {
        var employee = NewEmployee(_employeeId, "Carlos Ruiz");
        var employees = new FakeEmployeeRepository().WithEmployee(employee);
        var handler = new ListVisibleRequestsHandler(
            new FakeCurrentUser(_employeeId, EmployeeRole.Employee),
            new ThrowingOnPendingForManagerRequestRepository(),
            employees,
            new FakeAbsenceTypeRepository(_type));

        var result = await handler.Handle(CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_Returns_An_Empty_List_When_Nothing_Is_Visible()
    {
        var employee = NewEmployee(_employeeId, "Carlos Ruiz");
        var requests = new FakeRequestRepository();
        var employees = new FakeEmployeeRepository().WithEmployee(employee);
        var handler = CreateHandler(requests, employees, _employeeId, EmployeeRole.Employee);

        var result = await handler.Handle(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    /// <summary>
    /// ListByIdsAsync must resolve a historical request's absence type even
    /// when the type is no longer returned by ListActiveAsync — the fake
    /// mirrors the same contract as the real repository (D7): no IsActive
    /// filter, unlike ListActiveAsync.
    /// </summary>
    [Fact]
    public async Task Handle_Resolves_The_Absence_Type_Name_Regardless_Of_ListActiveAsync_Filtering()
    {
        var employee = NewEmployee(_employeeId, "Carlos Ruiz");
        var otherType = AbsenceType.Create(new AbsenceTypeId(Guid.NewGuid()), AbsenceTypeCode.SickLeave, "Sick Leave").Value;
        var request = NewRequest(_employeeId, NowUtc, otherType.Id);
        var requests = new FakeRequestRepository(request);
        var employees = new FakeEmployeeRepository().WithEmployee(employee);
        var handler = new ListVisibleRequestsHandler(
            new FakeCurrentUser(_employeeId, EmployeeRole.Employee),
            requests,
            employees,
            new FakeAbsenceTypeRepository(_type, otherType));

        var result = await handler.Handle(CancellationToken.None);

        Assert.True(result.IsSuccess);
        var dto = Assert.Single(result.Value);
        Assert.Equal("Sick Leave", dto.AbsenceType.Name);
    }

    /// <summary>Fails the test if the Employee role branch is ever taken (AC5's server-side-only filter).</summary>
    private sealed class ThrowingOnPendingForManagerRequestRepository : Application.Abstractions.IRequestRepository
    {
        public Task<Request?> GetByIdAsync(RequestId id, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public void Add(Request request) => throw new NotSupportedException();

        public Task<IReadOnlyList<Request>> ListOwnedByAsync(EmployeeId owner, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Request>>([]);

        public Task<IReadOnlyList<Request>> ListPendingForManagerAsync(EmployeeId manager, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Employee callers must never reach the manager queue query.");
    }
}
