using BigSolutions.VacaFlow.Application.Requests;
using BigSolutions.VacaFlow.Application.UnitTests.Authentication.Fakes;
using BigSolutions.VacaFlow.Application.UnitTests.Requests.Fakes;
using BigSolutions.VacaFlow.Domain.AbsenceTypes;
using BigSolutions.VacaFlow.Domain.Employees;
using BigSolutions.VacaFlow.Domain.Requests;

namespace BigSolutions.VacaFlow.Application.UnitTests.Requests;

public sealed class CancelRequestHandlerTests
{
    private static readonly EmployeeId OwnerId = new(Guid.NewGuid());
    private static readonly DateTimeOffset FixedNow = new(2026, 8, 10, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 8, 10);
    private static readonly AbsenceTypeId TypeId = new(Guid.NewGuid());

    private Request NewDraft(DateOnly? startDate = null) => Request.Create(
        new RequestId(Guid.NewGuid()), OwnerId, TypeId,
        DateRange.Create(startDate ?? Today, (startDate ?? Today).AddDays(2)).Value, "Family trip",
        Today, FixedNow.UtcDateTime).Value;

    private CancelRequestHandler CreateHandler(
        FakeRequestRepository requests,
        EmployeeId? actingUser = null,
        DateTimeOffset? now = null) =>
        new(
            new FakeCurrentUser(actingUser ?? OwnerId),
            requests,
            new FakeUnitOfWork(),
            new FixedTimeProvider(now ?? FixedNow));

    [Fact]
    public async Task Handle_Should_Succeed_When_The_Owner_Cancels_Their_Own_Draft()
    {
        var draft = NewDraft(Today.AddDays(5));
        var requests = new FakeRequestRepository(draft);
        var handler = CreateHandler(requests);

        var result = await handler.Handle(draft.Id.Value, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(RequestState.Cancelled, draft.State);
        Assert.Equal(FixedNow.UtcDateTime, draft.ClosedAtUtc);
    }

    [Fact]
    public async Task Handle_Should_Succeed_When_The_Owner_Cancels_Their_Own_Submitted_Request()
    {
        var draft = NewDraft();
        draft.Submit(Today, FixedNow.UtcDateTime);
        var requests = new FakeRequestRepository(draft);
        var handler = CreateHandler(requests);

        var result = await handler.Handle(draft.Id.Value, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(RequestState.Cancelled, draft.State);
        Assert.NotNull(draft.SubmittedAtUtc);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Request_Does_Not_Exist()
    {
        var requests = new FakeRequestRepository();
        var handler = CreateHandler(requests);

        var result = await handler.Handle(Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VF-REQ-006", result.Error.Code);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Caller_Is_Not_The_Owner()
    {
        var draft = NewDraft();
        var requests = new FakeRequestRepository(draft);
        var handler = CreateHandler(requests, actingUser: new EmployeeId(Guid.NewGuid()));

        var result = await handler.Handle(draft.Id.Value, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VF-REQ-004", result.Error.Code);
        Assert.Equal(RequestState.Draft, draft.State);
    }

    /// <summary>
    /// A non-owner of a request that is already Cancelled still gets 403,
    /// not 409 — ownership is decided before the transition itself (S1).
    /// </summary>
    [Fact]
    public async Task Handle_Should_Fail_With_Not_Owner_Even_When_The_Request_Is_Already_Cancelled()
    {
        var draft = NewDraft();
        draft.Cancel(FixedNow.UtcDateTime);
        var requests = new FakeRequestRepository(draft);
        var handler = CreateHandler(requests, actingUser: new EmployeeId(Guid.NewGuid()));

        var result = await handler.Handle(draft.Id.Value, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VF-REQ-004", result.Error.Code);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Request_Is_Already_Cancelled()
    {
        var draft = NewDraft();
        draft.Cancel(FixedNow.UtcDateTime);
        var closedAt = draft.ClosedAtUtc;
        var requests = new FakeRequestRepository(draft);
        var handler = CreateHandler(requests, now: FixedNow.AddHours(1));

        var result = await handler.Handle(draft.Id.Value, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VF-REQ-005", result.Error.Code);
        Assert.Equal(closedAt, draft.ClosedAtUtc);
    }
}
