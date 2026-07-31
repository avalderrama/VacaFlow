using BigSolutions.VacaFlow.Application.Requests;
using BigSolutions.VacaFlow.Application.UnitTests.AbsenceTypes.Fakes;
using BigSolutions.VacaFlow.Application.UnitTests.Authentication.Fakes;
using BigSolutions.VacaFlow.Application.UnitTests.Requests.Fakes;
using BigSolutions.VacaFlow.Domain.AbsenceTypes;
using BigSolutions.VacaFlow.Domain.Employees;
using BigSolutions.VacaFlow.Domain.Requests;

namespace BigSolutions.VacaFlow.Application.UnitTests.Requests;

public sealed class UpdateRequestHandlerTests
{
    private static readonly EmployeeId OwnerId = new(Guid.NewGuid());
    private static readonly DateTimeOffset FixedNow = new(2026, 8, 10, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 8, 10);

    private readonly AbsenceType _seededType =
        AbsenceType.Create(new AbsenceTypeId(Guid.NewGuid()), AbsenceTypeCode.Vacation, "Vacation").Value;

    private Request NewDraft() => Request.Create(
        new RequestId(Guid.NewGuid()), OwnerId, _seededType.Id,
        DateRange.Create(Today, Today.AddDays(2)).Value, "Family trip",
        Today, FixedNow.UtcDateTime).Value;

    private UpdateRequestHandler CreateHandler(
        FakeRequestRepository requests,
        EmployeeId? actingUser = null,
        AbsenceType? seededType = null) =>
        new(
            new FakeCurrentUser(actingUser ?? OwnerId),
            new FakeAbsenceTypeRepository(seededType ?? _seededType),
            requests,
            new FakeUnitOfWork(),
            new FixedTimeProvider(FixedNow));

    private UpdateRequestCommand ValidCommand(Guid requestId) => new(
        requestId, _seededType.Id.Value, Today, Today.AddDays(2), "Updated reason");

    [Fact]
    public async Task Handle_Should_Succeed_With_Valid_Data()
    {
        var draft = NewDraft();
        var requests = new FakeRequestRepository(draft);
        var handler = CreateHandler(requests);

        var result = await handler.Handle(ValidCommand(draft.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Updated reason", draft.Reason);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Request_Does_Not_Exist()
    {
        var requests = new FakeRequestRepository();
        var handler = CreateHandler(requests);

        var result = await handler.Handle(ValidCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VF-REQ-006", result.Error.Code);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Caller_Is_Not_The_Owner()
    {
        var draft = NewDraft();
        var requests = new FakeRequestRepository(draft);
        var handler = CreateHandler(requests, actingUser: new EmployeeId(Guid.NewGuid()));

        var result = await handler.Handle(ValidCommand(draft.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VF-REQ-004", result.Error.Code);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Absence_Type_Is_Missing()
    {
        var draft = NewDraft();
        var requests = new FakeRequestRepository(draft);
        var command = ValidCommand(draft.Id.Value) with { AbsenceTypeId = null };
        var handler = CreateHandler(requests);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VF-VAL-001", result.Error.Code);
        Assert.Equal("absenceTypeId", result.Error.Field);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Start_Date_Is_Missing()
    {
        var draft = NewDraft();
        var requests = new FakeRequestRepository(draft);
        var command = ValidCommand(draft.Id.Value) with { StartDate = null };
        var handler = CreateHandler(requests);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("startDate", result.Error.Field);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_End_Date_Is_Missing()
    {
        var draft = NewDraft();
        var requests = new FakeRequestRepository(draft);
        var command = ValidCommand(draft.Id.Value) with { EndDate = null };
        var handler = CreateHandler(requests);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("endDate", result.Error.Field);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Reason_Is_Missing()
    {
        var draft = NewDraft();
        var requests = new FakeRequestRepository(draft);
        var command = ValidCommand(draft.Id.Value) with { Reason = null };
        var handler = CreateHandler(requests);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("reason", result.Error.Field);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_End_Date_Is_Before_The_Start_Date()
    {
        var draft = NewDraft();
        var requests = new FakeRequestRepository(draft);
        var command = ValidCommand(draft.Id.Value) with { EndDate = Today.AddDays(-1) };
        var handler = CreateHandler(requests);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VF-REQ-001", result.Error.Code);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Start_Date_Is_In_The_Past()
    {
        var draft = NewDraft();
        var requests = new FakeRequestRepository(draft);
        var yesterday = Today.AddDays(-1);
        var command = ValidCommand(draft.Id.Value) with { StartDate = yesterday, EndDate = yesterday };
        var handler = CreateHandler(requests);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VF-REQ-002", result.Error.Code);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Absence_Type_Does_Not_Exist()
    {
        var draft = NewDraft();
        var requests = new FakeRequestRepository(draft);
        var otherType = AbsenceType.Create(new AbsenceTypeId(Guid.NewGuid()), AbsenceTypeCode.SickLeave, "Sick Leave").Value;
        var handler = CreateHandler(requests, seededType: otherType);

        var result = await handler.Handle(ValidCommand(draft.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VF-CAT-001", result.Error.Code);
    }

    [Fact]
    public async Task Handle_Should_Succeed_When_The_Start_Date_Equals_Today()
    {
        var draft = NewDraft();
        var requests = new FakeRequestRepository(draft);
        var command = ValidCommand(draft.Id.Value) with { StartDate = Today, EndDate = Today };
        var handler = CreateHandler(requests);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }
}
